db-migrate:
	dotnet ef migrations add $(var)
db-update:
	dotnet ef database update
build:
	docker build -t sketch-blend-user-module .