// Code generated by goctl. DO NOT EDIT.
package types

type LogReq struct {
	ProjectName   string `form:"ProjectName"`
	AppName       string `form:"AppName"`
	AppVersion    string `form:"AppVersion"`
	ResourceInfo  string `form:"ResourceInfo"`
	ERRTime       int64  `form:"ERRTime"`
	ERRType       string `form:"ERRType"`
	ERRLog        string `form:"ERRLog"`
	ERRStackTrace string `form:"ERRStackTrace"`
}

type LogResp struct {
	Status string `json:"ProjectName"`
}