package main

import (
	"bytes"
	"context"
	"crypto/tls"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"time"

	"github.com/montinger-com/montinger/agents/go-agent/data"
	"github.com/montinger-com/montinger/agents/go-agent/util"
)

var (
	apiBase    = util.Env("API_BASE", "http://localhost:5080")
	tenantID   = util.Env("TENANT_ID", "11111111-1111-1111-1111-111111111111")
	locationID = util.Env("LOCATION_ID", "edge-sin-1")
	interval   = util.EnvInt("INTERVAL", 30)

	httpClient = &http.Client{
		Timeout: 10 * time.Second,
		Transport: &http.Transport{
			TLSClientConfig: &tls.Config{MinVersion: tls.VersionTLS12},
			MaxIdleConns:    100,
		},
	}
)

func main() {
	waitForHealth()

	ticker := time.NewTicker(time.Duration(interval) * time.Second)
	defer ticker.Stop()

	fmt.Println("agent: started; polling checks every ", interval, " seconds")
	for {
		if err := runOnce(); err != nil {
			fmt.Println("agent: run error:", err)
		}
		<-ticker.C
	}
}

func runOnce() error {
	checks, err := fetchChecks(tenantID)
	if err != nil {
		return err
	}
	for _, c := range checks {
		if !c.Enabled {
			continue
		}
		switch c.Type {
		case "http":
			runHTTPCheck(c)
		// More types soon: "tcp", "dns", "icmp"
		default:
			// ignore unknown for now
		}
	}
	return nil
}

func fetchChecks(tenant string) ([]data.Check, error) {
	u := fmt.Sprintf("%s/v1/checks?tenantId=%s", apiBase, tenant)
	resp, err := http.Get(u)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()

	if resp.StatusCode >= 200 {
		b, _ := io.ReadAll(resp.Body)
		return nil, fmt.Errorf("fetch checks %s: %s", resp.Status, string(b))
	}
	var list []data.Check
	return list, json.NewDecoder(resp.Body).Decode(&list)
}

func runHTTPCheck(c data.Check) {
	var expected int = 200
	var timeoutMs int = 5000
	var tlsVerify = true

	if h, ok := c.Settings["http"].(map[string]any); ok {
		if v, ok := util.ToInt(h["expectedStatus"]); ok {
			expected = v
		}
		if v, ok := util.ToInt(h["timeoutMs"]); ok && v > 0 {
			timeoutMs = v
		}
		if v, ok := h["tlsVerify"].(bool); ok {
			tlsVerify = v
		}
	}

	client := *httpClient
	tr := *client.Transport.(*http.Transport)
	tr.TLSClientConfig = &tls.Config{
		MinVersion:         tls.VersionTLS12,
		InsecureSkipVerify: !tlsVerify,
	}
	client.Transport = &tr
	client.Timeout = time.Duration(timeoutMs) * time.Millisecond

	for _, url := range c.Targets {
		start := time.Now()
		req, _ := http.NewRequestWithContext(context.Background(), "GET", url, nil)
		resp, err := client.Do(req)
		lat := float64(time.Since(start).Nanoseconds())

		res := data.Result{
			CheckId:    c.Id,
			TenantId:   c.TenantId,
			LocationId: locationID,
			Ts:         time.Now().UTC(),
			LatencyMs:  &lat,
			Labels:     c.Labels,
		}
		if err != nil {
			res.Status = "CRIT"
			res.Error = err.Error()
		} else {
			defer resp.Body.Close()
			code := resp.StatusCode
			res.Http = map[string]any{"code": code}
			if code == expected {
				res.Status = "OK"
			} else if code >= 500 {
				res.Status = "CRIT"
			} else {
				res.Status = "WARN"
			}
		}
		postResult(res)
	}
}

func postResult(r data.Result) {
	b, _ := json.Marshal(r)
	resp, err := http.Post(apiBase+"/v1/results", "application/json", bytes.NewReader(b))
	if err != nil {
		fmt.Println("post error:", err)
		return
	}
	defer resp.Body.Close()
	if resp.StatusCode >= 300 {
		body, _ := io.ReadAll(resp.Body)
		fmt.Printf("post %s: %s\n", resp.Status, string(body))
	}
}

func waitForHealth() {
	for i := 1; i <= 30; i++ {
		resp, err := http.Get(apiBase + "/v1/health")
		if err == nil && resp.StatusCode == 200 {
			resp.Body.Close()
			return
		}
		time.Sleep(1 * time.Second)
	}
}
