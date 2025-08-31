package data

type Check struct {
	Id       string            `json:"id"`
	TenantId string            `json:"tenantId"`
	Name     string            `json:"name"`
	Type     string            `json:"type"`
	Enabled  bool              `json:"enabled"`
	Schedule string            `json:"schedule"`
	Targets  []string          `json:"targets"`
	Labels   map[string]string `json:"labels"`
	Settings map[string]any    `json:"settings"`
}
