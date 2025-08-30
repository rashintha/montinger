package main

import (
	"fmt"
	"net/http"
	"os"
)

func main() {
	api := env("API_BASE", "http://localhost:5080")

	resp, err := http.Get(api + "/v1/health")
	if err != nil {
		fmt.Println("Health Error:", err)
		return
	}
	fmt.Println("Health Response:", resp.Status)
}

func env(k, d string) string {
	if v := os.Getenv(k); v != "" {
		return v
	}
	return d
}
