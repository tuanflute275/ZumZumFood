# Deploy with docker
	
	> http://localhost:8080/swagger/index.html
	> http://localhost:8080/health
	
	step 1: remove file .git
	step2 : refactor appSetting.json
	step 3: build
	
# Grafana

	Dashboard => new Dashboard => create visual => header (view) // body (queries) => gọi tới job hoặc api để thống kê => run queries

# AppSetting.json

	{
	  "Logging": {
		"LogLevel": {
		  "Default": "Information",
		  "Microsoft.AspNetCore": "Warning"
		}
	  },
	  "AllowedHosts": "*",
	   "ConnectionStrings": {
		"DefaultConnection": "Server=host.docker.internal,1433; Database=ZumZumFood; User Id=sa; Password=Admin@1234; TrustServerCertificate=True; MultipleActiveResultSets=True;"
	  },
	  "CacheConnection": {
		"RedisServer": "host.docker.internal:6379"
	  },
	  "Authentication": {
		"Google": {
		  "ClientId": "your-clientId",
		  "ClientSecret": "your-ClientSecret"
		},
		"Facebook": {
		  "AppId": "your-AppId",
		  "AppSecret": "your-AppSecret"
		}
	  },
	  "Jwt": {
		"Issuer": "tuanflute275.vn",
		"Audience": "tuanflute275",
		"Key": "A3f7Qz9wXfH2n6pR0gV7aM8sVt9Pz8jR0tP3x5yZbQ3"
	  },
	  "EmailConfiguration": {
		"From": "your-email@gmail.com",
		"SmtpServer": "smtp.gmail.com",
		"Port": 587,
		"UserName": "your-email@gmail.com",
		"Password": "your-password"
	  },
	  "Serilog": {
		"Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
		"MinimumLevel": {
		  "Default": "Information",
		  "Override": {
			"Microsoft": "Warning",
			"System": "Warning"
		  }
		},
		"WriteTo": [
		  {
			"Name": "Console"
		  },
		  {
			"Name": "File",
			"Args": {
			  "path": "Logs/log-.txt",
			  "rollingInterval": "Day",
			  "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
			}
		  }
		],
		"Enrich": [ "FromLogContext" ],
		"Properties": {
		  "Application": "MyApp"
		}
	  }
	}