package util

import "strconv"

func ToInt(v any) (int, bool) {
	switch t := v.(type) {
	case float64:
		return int(t), true
	case int:
		return t, true
	case string:
		i, err := strconv.Atoi(t)
		return i, err == nil
	default:
		return 0, false
	}
}
