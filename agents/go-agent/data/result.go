package data

import "time"

type Result struct {
	ResultId   string            `json:"resultId,omitempty"`
	CheckId    string            `json:"checkId"`
	TenantId   string            `json:"tenantId"`
	LocationId string            `json:"locationId"`
	Status     string            `json:"status"`
	Ts         time.Time         `json:"ts"`
	LatencyMs  *float64          `json:"latencyMs,omitempty"`
	Http       map[string]any    `json:"http,omitempty"`
	Dns        map[string]any    `json:"dns,omitempty"`
	Icmp       map[string]any    `json:"icmp,omitempty"`
	Tcp        map[string]any    `json:"tcp,omitempty"`
	Error      string            `json:"error,omitempty"`
	Labels     map[string]string `json:"labels,omitempty"`
}
