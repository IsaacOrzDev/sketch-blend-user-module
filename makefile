db-migrate:
	dotnet ef migrations add InitialCreate
db-update:
	dotnet ef database update
build:
	docker build -t demo-system-sub .