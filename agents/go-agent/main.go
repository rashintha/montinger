package main

import (
	"bytes"
	"context"
	"crypto/tls"
	"encoding/json"
	"fmt"
	"io"
	"net"
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
		fmt.Println("check:", c.Id, c.Name, c.Enabled, c.Type)
		if !c.Enabled {
			continue
		}
		switch c.Type {
		case "http":
			runHTTPCheck(c)
		case "tcp":
			runTCPCheck(c)
		case "dns":
			runDNSCheck(c)
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

	if resp.StatusCode >= 300 {
		b, _ := io.ReadAll(resp.Body)
		return nil, fmt.Errorf("fetch checks %s: %s", resp.Status, string(b))
	}

	b, err := io.ReadAll(resp.Body)
	if err != nil {
		// If server returned 200 but closed early, b might be empty — treat as no checks
		if err == io.EOF {
			return []data.Check{}, nil
		}
		return nil, err
	}
	if len(b) == 0 {
		return []data.Check{}, nil
	}

	var list []data.Check
	if err := json.Unmarshal(b, &list); err != nil {
		return nil, fmt.Errorf("parse checks json: %w; body=%q", err, string(b))
	}
	return list, nil
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
		lat := float64(time.Since(start).Milliseconds())

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

func runTCPCheck(c data.Check) {
	var port int
	var timeoutMs = 3000
	if t, ok := c.Settings["tcp"].(map[string]any); ok {
		if v, ok := util.ToInt(t["port"]); ok {
			port = v
		}
		if v, ok := util.ToInt(t["timeoutMs"]); ok && v > 0 {
			timeoutMs = v
		}
	}
	if port <= 0 {
		// No port defined -> produce a CRIT result per target
		for _, _ = range c.Targets {
			postResult(data.Result{
				CheckId: c.Id, TenantId: c.TenantId, LocationId: locationID,
				Ts: time.Now().UTC(), Status: "CRIT",
				Error: "tcp.port not specified", Labels: c.Labels,
			})
		}
		return
	}

	for _, host := range c.Targets {
		addr := fmt.Sprintf("%s:%d", host, port)
		start := time.Now()
		conn, err := net.DialTimeout("tcp", addr, time.Duration(timeoutMs)*time.Millisecond)
		lat := float64(time.Since(start).Milliseconds())

		res := data.Result{
			CheckId: c.Id, TenantId: c.TenantId, LocationId: locationID,
			Ts: time.Now().UTC(), LatencyMs: &lat, Labels: c.Labels,
		}
		if err != nil {
			res.Status = "CRIT"
			res.Error = err.Error()
		} else {
			_ = conn.Close()
			res.Status = "OK"
			res.Tcp = map[string]any{"handshakeMs": lat}
		}
		postResult(res)
	}
}

func runDNSCheck(c data.Check) {
	// defaults
	rtype := "A"
	resolver := "1.1.1.1:53"
	timeoutMs := 2000

	if d, ok := c.Settings["dns"].(map[string]any); ok {
		if v, ok := d["recordType"].(string); ok && v != "" {
			rtype = v
		}
		if v, ok := d["resolver"].(string); ok && v != "" {
			resolver = v
		}
		if v, ok := util.ToInt(d["timeoutMs"]); ok && v > 0 {
			timeoutMs = v
		}
	}

	// Custom resolver using UDP to chosen server
	dialer := &net.Dialer{Timeout: time.Duration(timeoutMs) * time.Millisecond}
	res := &net.Resolver{
		PreferGo: true, // use Go’s resolver with Dial override
		Dial: func(ctx context.Context, network, address string) (net.Conn, error) {
			// always use our resolver
			return dialer.DialContext(ctx, "udp", resolver)
		},
	}

	for _, name := range c.Targets {
		ctx, cancel := context.WithTimeout(context.Background(), time.Duration(timeoutMs)*time.Millisecond)
		start := time.Now()
		var err error
		var anyFound bool

		switch rtype {
		case "A":
			var addrs []string
			addrs, err = res.LookupHost(ctx, name)
			anyFound = len(addrs) > 0
		case "AAAA":
			var ips []net.IPAddr
			ips, err = res.LookupIPAddr(ctx, name)
			// Filter to v6 presence
			for _, ip := range ips {
				if ip.IP.To16() != nil && ip.IP.To4() == nil {
					anyFound = true
					break
				}
			}
		case "TXT":
			var txts []string
			txts, err = res.LookupTXT(ctx, name)
			anyFound = len(txts) > 0
		case "CNAME":
			var cname string
			cname, err = res.LookupCNAME(ctx, name)
			anyFound = cname != ""
		default:
			err = fmt.Errorf("unsupported recordType: %s", rtype)
		}
		cancel()

		lat := float64(time.Since(start).Milliseconds())
		r := data.Result{
			CheckId: c.Id, TenantId: c.TenantId, LocationId: locationID,
			Ts: time.Now().UTC(), LatencyMs: &lat, Labels: c.Labels,
			Dns: map[string]any{"recordType": rtype},
		}
		if err != nil {
			r.Status = "CRIT"
			r.Error = err.Error()
		} else if !anyFound {
			r.Status = "WARN"
		} else {
			r.Status = "OK"
		}
		postResult(r)
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

	fmt.Println("post result:", resp.StatusCode, r.CheckId, r.TenantId, r.LocationId, r.Ts, r.Status, r.Error, r.LatencyMs, r.Labels, r.Http)

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
