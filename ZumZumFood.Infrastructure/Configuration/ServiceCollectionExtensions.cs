﻿namespace ZumZumFood.Infrastructure.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDerivativeTradeServices(this IServiceCollection services, IConfiguration configuration)
        {
            services
               .AddSerilogConfiguration(configuration)
               .AddSqlServerConfiguration(configuration)
               .AddCorsConfiguration()
               .AddAutoMapperConfiguration()
               .AddSingletonServices(configuration)
               .AddEmailConfiguration(configuration)
               .AddJwtConfiguration(configuration)
               .AddCacheConfiguration(configuration)
               .AddOauth2Configuration(configuration)
               .AddRabbitMQConfiguration(configuration)
               .AddElasticSearchConfiguration(configuration)
               .AddTransientServices();
            return services;
        }

        public static void AddTransientServices(this IServiceCollection services)
        {
            services.AddTransient<IPDFService, PDFService>();
            services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
            services.AddTransient<ISQLQueryHandler, SQLQueryHandler>();
            services.AddTransient<IUnitOfWork, UnitOfWork>();
            services.AddTransient<IEmailService, EmailService>();
            services.AddTransient<IAuthService, AuthService>();
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<ILogService, LogService>();
            services.AddTransient<IParameterService, ParameterService>();
            services.AddTransient<IBannerService, BannerService>();
            services.AddTransient<ICategoryService, CategoryService>();
            services.AddTransient<IBrandService, BrandService>();
            services.AddTransient<IProductService, ProductService>();
            services.AddTransient<IProductCommentService, ProductCommentService>();
            services.AddTransient<IProductImageService, ProductImageService>();
            services.AddTransient<IProductDetailService, ProductDetailService>();
            services.AddTransient<IWishlistService, WishlistService>();
            services.AddTransient<ICartService, CartService>();
            services.AddTransient<ICouponService, CouponService>();
            services.AddTransient<ICouponConditionService, CouponConditionService>();
            services.AddTransient<IComboService, ComboService>();
            services.AddTransient<IOrderService, OrderService>();
            services.AddTransient<ICodeService, CodeService>();
        }

        // Add singleton
        private static IServiceCollection AddSingletonServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Đăng ký IHttpContextAccessor
            services.AddHttpContextAccessor();
            services.AddHealthChecks()
                .AddSqlServer(
                    connectionString: configuration.GetConnectionString("DefaultConnection"),
                    name: "SQL Server",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: new[] { "db", "sql" })
                .AddUrlGroup(new Uri("http://localhost:8080/api/v1/user/1"), name: "User API")
                .AddCheck("Custom Check", () => HealthCheckResult.Healthy("All systems operational"));

            return services;
        }

        // Cấu hình dịch vụ email
        public static IServiceCollection AddEmailConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind cấu hình Email từ appsettings.json
            var emailConfig = configuration.GetSection("EmailConfiguration").Get<EmailModel>();

            // Đăng ký cấu hình email như một singleton
            services.AddSingleton(emailConfig);
            return services;
        }

        // Đăng ký các dịch vụ scoped
        public static IServiceCollection AddSqlServerConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            return services;
        }

        // Cấu hình AutoMapper
        public static IServiceCollection AddAutoMapperConfiguration(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(AutomapConfig));
            return services;
        }

        // Cấu hình CORS
        public static IServiceCollection AddCorsConfiguration(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowOrigin", policy =>
                {
                    policy  // Cho phép nguồn từ localhost:3000
                           .WithOrigins("http://localhost:3000", "http://localhost:4200")
                          .AllowAnyHeader()                         // Cho phép bất kỳ header nào
                          .AllowAnyMethod()                        // Cho phép bất kỳ phương thức HTTP nào
                          .AllowCredentials();                    // Cho phép cookies hoặc thông tin xác thực khác
                });
            });
            return services;
        }

        // Cấu hình logging (Serilog)
        public static IServiceCollection AddSerilogConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory); // Tạo thư mục nếu chưa tồn tại
            }

            // Cấu hình Serilog
            Serilog.Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)  // Đọc cấu hình từ appsettings.json
                .Enrich.FromLogContext()                // Thêm ngữ cảnh log
                .WriteTo.Console()                      // Ghi log ra Console
                .WriteTo.File(
                    Path.Combine(logDirectory, "log-.txt"),  // Đường dẫn tới thư mục LogFileDirectory
                    rollingInterval: RollingInterval.Day,     // Log mỗi ngày vào file mới
                    retainedFileCountLimit: 7                // Giới hạn số file log giữ lại (ví dụ 7 ngày)
                )
                .CreateLogger();
            services.AddSingleton<ILogger>(Serilog.Log.Logger);
            return services;
        }

        // Cấu hình bộ nhớ cache (Redis hoặc fallback MemoryCache)
        public static IServiceCollection AddCacheConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            // Cấu hình kết nối Redis với fallback sử dụng bộ nhớ nếu Redis không khả dụng
            var redisConnectionString = configuration["CacheConnection:RedisServer"];
            var isRedisConnected = false;
            IConnectionMultiplexer redis = null;

            try
            {
                // Thử kết nối Redis
                redis = ConnectionMultiplexer.Connect(redisConnectionString);
                isRedisConnected = redis.IsConnected;
                Constant.IsRedisConnectedStatic = redis.IsConnected;
            }
            catch (Exception ex)
            {
                // Ghi log nếu không thể kết nối Redis
                Console.WriteLine($"Không thể kết nối Redis: {ex.Message}");
            }
            finally
            {
                redis?.Dispose();
            }

            if (isRedisConnected)
            {
                // Nếu kết nối Redis thành công, sử dụng RedisCache
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnectionString;
                });

                // Đảm bảo sử dụng RedisCacheService với kết nối Redis
                services.AddScoped<IRedisCacheService, RedisCacheService>(sp =>
                    new RedisCacheService(redisConnectionString, null));
            }
            else
            {
                // Nếu không thể kết nối Redis, sử dụng MemoryCache (fallback)
                Console.WriteLine("Kết nối Redis thất bại, sử dụng MemoryCache.");
                services.AddMemoryCache(); // Thêm MemoryCache nếu không kết nối được Redis
                                           // Đảm bảo fallback sử dụng MemoryCache trong RedisCacheService
                services.AddScoped<IRedisCacheService, RedisCacheService>(sp =>
                    new RedisCacheService(redisConnectionString, sp.GetRequiredService<IMemoryCache>()));
            }
            return services;
        }

        // Cấu hình Oauth2
        public static IServiceCollection AddOauth2Configuration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                //options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

            })
            .AddGoogle(options =>
            {
                options.ClientId = configuration["Authentication:Google:ClientId"];
                options.ClientSecret = configuration["Authentication:Google:ClientSecret"];
                options.CallbackPath = "/api/v1/auth/google-callback-2";
                options.SaveTokens = true;
                options.Scope.Add("email");
                options.Scope.Add("profile");
            })
            .AddFacebook(options =>
            {
                options.AppId = configuration["Authentication:Facebook:AppId"];
                options.AppSecret = configuration["Authentication:Facebook:AppSecret"];
                options.Scope.Add("public_profile");
                options.Fields.Add("picture");
                options.Scope.Add("email");
                options.Fields.Add("email");
            });

            return services;
        }

        // Cấu hình JWT
        public static IServiceCollection AddJwtConfiguration(this IServiceCollection services, IConfiguration configuration)
         {
             var key = configuration["Jwt:Key"];
             var signingKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key));

             services.AddAuthentication(options =>
             {
                 options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                 options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                 options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
             })
             .AddJwtBearer(options =>
             {
                 options.SaveToken = true;
                 options.RequireHttpsMetadata = false;  // Cài đặt này cần bật khi triển khai ứng dụng thực tế
                 options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                 {
                     ValidateIssuer = true,
                     ValidIssuer = configuration["Jwt:Issuer"],  // Lấy thông tin từ appsettings.json

                     ValidateAudience = true,
                     ValidAudience = configuration["Jwt:Audience"],  // Lấy thông tin từ appsettings.json

                     IssuerSigningKey = signingKey,  // Đăng ký khóa ký cho JWT

                     RequireExpirationTime = true,
                     ValidateLifetime = true,  // Kiểm tra thời gian sống của token
                 };

                 options.Events = new JwtBearerEvents
                 {
                     // Xử lý sự kiện khi không có token hoặc token không hợp lệ
                     OnChallenge = context =>
                     {
                         // Bỏ qua phản hồi mặc định của JWT Bearer
                         context.HandleResponse();

                         // Thiết lập trạng thái và kiểu nội dung phản hồi
                         context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                         context.Response.ContentType = "application/json";

                         // Tạo một đối tượng ResponseObject với mã lỗi và thông điệp tùy chỉnh
                         var result = JsonSerializer.Serialize(new ResponseObject(401, "Unauthorized. Token is invalid or missing."));

                         // Trả về phản hồi lỗi tùy chỉnh dưới dạng JSON
                         return context.Response.WriteAsync(result);
                     },

                     // Xử lý sự kiện khi token hợp lệ nhưng người dùng không có quyền truy cập vào tài nguyên
                     OnForbidden = context =>
                     {
                         // Thiết lập trạng thái và kiểu nội dung phản hồi
                         context.Response.StatusCode = StatusCodes.Status403Forbidden;
                         context.Response.ContentType = "application/json";

                         // Tạo một đối tượng ResponseObject với mã lỗi và thông điệp tùy chỉnh
                         var result = JsonSerializer.Serialize(new ResponseObject(403, "Forbidden. You do not have permission to access this resource."));

                         // Trả về phản hồi lỗi tùy chỉnh dưới dạng JSON
                         return context.Response.WriteAsync(result);
                     }
                 };
             });
             return services;
         }

        // Cấu hình rabbitMQ
        public static IServiceCollection AddRabbitMQConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IRabbitService, RabbitService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<RabbitService>>();
                var rabbitSettings = configuration.GetSection(nameof(RabbitSetting)).Get<List<RabbitSetting>>();

                // Kiểm tra nếu cấu hình trống hoặc có vấn đề gì đó
                if (rabbitSettings == null || !rabbitSettings.Any())
                {
                    logger.LogError("RabbitMQ settings not found in configuration.");
                    throw new InvalidOperationException("RabbitMQ settings are required.");
                }

                // Lấy cấu hình cho HNX và FixReceive
                var configHNX = rabbitSettings.FirstOrDefault(e => e.Id.Equals(Constant.HNXSettingId));
                var configFixReceive = rabbitSettings.FirstOrDefault(e => e.Id.Equals(Constant.FixReceiveSettingId));

                if (configHNX == null || configFixReceive == null)
                {
                    logger.LogError("RabbitMQ configuration for HNX or FixReceive not found.");
                    throw new InvalidOperationException("Required RabbitMQ settings not found.");
                }

                // Tạo ConnectionFactory cho HNX và FixReceive
                var factoryHNX = new ConnectionFactory()
                {
                    UserName = configHNX.UserName,
                    Password = configHNX.Password,
                    HostName = configHNX.HostName,
                };
                var factoryFixReceive = new ConnectionFactory()
                {
                    UserName = configFixReceive.UserName,
                    Password = configFixReceive.Password,
                    HostName = configFixReceive.HostName,
                };

                // Trả về dịch vụ RabbitService với các cài đặt đã cấu hình
                return new RabbitService(factoryHNX, factoryFixReceive, logger, configHNX, configFixReceive);
            });

            // Đăng ký RabbitMqConsumer và HostedService
            //services.AddSingleton<RabbitMqConsumer>();
            //services.AddHostedService<RabbitMqBackgroundService>();

            return services;
        }


        // Cấu hình dịch vụ elasticSearch
        public static IServiceCollection AddElasticSearchConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var uri = configuration["Elasticsearch:Uri"];
            var index = configuration["Elasticsearch:Index"];

            var settings = new ConnectionSettings(new Uri(uri))
                           .DefaultIndex(index);

            var client = new ElasticClient(settings);

            services.AddSingleton<IElasticClient>(client);
            return services;
        }

    }
}
