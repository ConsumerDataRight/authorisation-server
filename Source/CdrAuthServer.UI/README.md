# Prerequisites
- install typescript - `npm install -g typescript`
- npm install @types/react

# Getting started
- Install dependencies - `npm install`
- Start the UI - `npm start`
- Start the UI in a local development environment for the Banking Mock Data Holder (default) - `npm run start-local`
- Start the UI in a local development environment for the Energy Mock Data Holder - `copy .env.development.energy .env.development; $env:PORT=3100; npm run start-local`\
  (Noting this is due to the known issue with Node 17+ as mentioned <a href="https://nodejs.org/en/blog/release/v17.0.0/">here</a>)

# Seed Data Files

## About
There are 3 seed data files
1. Customer and account data for banking (customer-seed-data.json). This has the same structure as the seed data file inside the banking data holder solution.
2. Customer and account data for energy (customer-seed-data-energy.json). This has the same structure as the seed data file inside the energy data holder solution with following modifications.
- replace AccountNumber with MaskedName
3. Cluster mapping data. E.g. cluster-seed-data.json

## Locations
- Load from Local File System - Copy 2 seed data files to the public folder and change the .env config setting with the correct file name. E.g.\
REACT_APP_DATA_FILE_NAME=customer-seed-data.json\
REACT_APP_CLUSTER_DATA_FILE_NAME=cluster-seed-data.json

- From Azure Storage - Place the files in a blob storage and change the corresponding .env confirm file with the correct URI. E.g.\
REACT_APP_DATA_FILE_NAME=`<<YourAzureStorageAccount>>`\
REACT_APP_CLUSTER_DATA_FILE_NAME=`<<YourAzureStorageAccount>>`

# Input params for the /login endpoint
- code - base64 encoded json string of the LoginParams object. E.g.\
/login?code=eyJhbGciOiJQUzI1NiIsImtpZCI6IjdDNTcxNjU1M0U5QjEzMkVGMzI1QzQ5Q0EyMDc5NzM3MTk2QzAzREIiLCJ4NXQiOiJmRmNXVlQ2YkV5N3pKY1Njb2dlWE54bHNBOXMiLCJ0eXAiOiJKV1QifQ.eyJsb2dpbl9wYXJhbXMiOiJ7XCJhdXRob3JpemVfcmVxdWVzdFwiOntcInJlcXVlc3RfdXJpXCI6XCJ1cm46NmI3ZmRmYmYtZmI5NS00M2VjLWIwZDctMjA0MTNmODY0MjIyXCIsXCJyZXNwb25zZV90eXBlXCI6XCJjb2RlIGlkX3Rva2VuXCIsXCJyZXNwb25zZV9tb2RlXCI6XCJcIixcImNsaWVudF9pZFwiOlwiMzQ2MTJkOWUtNzFmMi00MTk0LTk3NzYtZGViNDNiNDA4OTVhXCIsXCJyZWRpcmVjdF91cmlcIjpcIlwiLFwic2NvcGVcIjpcIm9wZW5pZCBwcm9maWxlIGNvbW1vbjpjdXN0b21lci5iYXNpYzpyZWFkIGNvbW1vbjpjdXN0b21lci5kZXRhaWw6cmVhZCBiYW5rOmFjY291bnRzLmJhc2ljOnJlYWQgYmFuazphY2NvdW50cy5kZXRhaWw6cmVhZCBiYW5rOnRyYW5zYWN0aW9uczpyZWFkIGJhbms6cmVndWxhcl9wYXltZW50czpyZWFkIGJhbms6cGF5ZWVzOnJlYWQgY2RyOnJlZ2lzdHJhdGlvblwiLFwibm9uY2VcIjpcIlwifSxcInJldHVybl91cmxcIjpcImh0dHBzOi8vbW9jay1kYXRhLWhvbGRlcjo4MDAxL2Nvbm5lY3QvYXV0aG9yaXplLWNhbGxiYWNrXCIsXCJkaF9icmFuZF9uYW1lXCI6XCJNb2NrIERhdGEgSG9sZGVyIEJhbmtpbmdcIixcImRoX2JyYW5kX2FiblwiOlwiNDggWFhYIFhYWFwiLFwiZHJfYnJhbmRfbmFtZVwiOlwiTXlCdWRnZXRIZWxwZXJcIixcImN1c3RvbWVyX2lkXCI6XCJrc21pdGhcIixcIm90cFwiOlwiMDAwNzg5XCIsXCJzY29wZVwiOlwib3BlbmlkIHByb2ZpbGUgY29tbW9uOmN1c3RvbWVyLmJhc2ljOnJlYWQgY29tbW9uOmN1c3RvbWVyLmRldGFpbDpyZWFkIGJhbms6YWNjb3VudHMuYmFzaWM6cmVhZCBiYW5rOmFjY291bnRzLmRldGFpbDpyZWFkIGJhbms6dHJhbnNhY3Rpb25zOnJlYWQgYmFuazpyZWd1bGFyX3BheW1lbnRzOnJlYWQgYmFuazpwYXllZXM6cmVhZCBjZHI6cmVnaXN0cmF0aW9uXCIsXCJzaGFyaW5nX2R1cmF0aW9uXCI6bnVsbH0iLCJuYmYiOjE2Nzc1NTM5OTksImV4cCI6MTY3NzU1NDI5OSwiaWF0IjoxNjc3NTUzOTk5LCJpc3MiOiJodHRwczovL21vY2stZGF0YS1ob2xkZXI6ODAwMSIsImF1ZCI6IjM0NjEyZDllLTcxZjItNDE5NC05Nzc2LWRlYjQzYjQwODk1YSJ9.Kp0_CfMBIjaXmYf3nqMlcsi8oMrpbPT8qCVmhhReLqtmd0l3GAM6I1sjOT4l0At3LHz5Rfylsiqq5SlqqFUmhmvEOlJE6MkGLPQCM64wV-pRpS7KdbBjNVt6C3SxAKQEJdgTp7zgJ85QE-iURfjJgBgoxkS8au65nWtlMEw2Yt9AOQezmBO0OkSkwObsVB59Mv2MSBQbMtOl_E0MqM_p_HehXZw9YJ1u-4UYGy0JlLyWVHCHX0GSN5EFlsVq8-VTORo5vrA1dnwuiZvC8Fm59jPV7gSAWQ6x8KgimQSp3EV3Zun8E3qNs2J6w8u10-wlMZMP9p9eyy4kNQPfDTILSOYx_x72LuK_PLX-vZvifStS7PZ_tDZseftT_Rs67IafdvzTg-Lg0Q5kmF5qPXr-0MsJT_o6hYgpIzEWprPY4XkNAT-rRCdJKphRJ_GYKYYAHT3387Z-l3PVJi77A7iwzPTzegOmgsmy_5i78oWw5eqYWsXr370pQPUot353tT0B