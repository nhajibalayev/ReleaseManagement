@echo off
rem Local demo: in-memory DB, Mock AD, Mock Azure DevOps, seeded users.
rem Login: admin / po / rm / qa / infosec / risk / chapterlead / techowner / dba / itops / devops / auditor
rem Password: ChangeMe!123   (any other user name with password Mock!123 also works via Mock AD)
cd /d "%~dp0"
rem stop a previously started demo so the port and build outputs are free
taskkill /F /IM ReleaseManagement.Web.exe >nul 2>&1
set ASPNETCORE_ENVIRONMENT=Development
set DemoMode=true
set Seed__Enabled=true
set WindowsAuth__Enabled=true
set WindowsAuth__Mode=Mock
set WindowsAuth__AllowLocalLogin=true
set AzureDevOps__Enabled=true
set AzureDevOps__Mode=Mock
set Hangfire__Enabled=false
echo Starting Release Management demo on http://localhost:5266 ...
start "" http://localhost:5266
dotnet run --project src\ReleaseManagement.Web --launch-profile http
pause
