﻿namespace ZumZumFood.Persistence.Repositories
{
    public class UnitOfWork : IDisposable, IUnitOfWork
    {
        private bool disposedValue;
        ApplicationDbContext _context;
        IDbContextTransaction _dbContextTransaction;
        // DI repository
        IUserRepository _userRepository;
        IRoleRepository _roleRepository;
        IUserRoleRepository _userRoleRepository;
        ITokenRepository _tokenRepository;
        IParameterRepository _parameterRepository;
        ILogRepository _logRepository;
        IBannerRepository _bannerRepository;
        ICategoryRepository _categoryRepository;
        IRestaurantRepository _restaurantRepository;
        IProductRepository _productRepository;
        IProductDetailRepository _productDetailRepository;
        IProductImageRepository _productImageRepository;
        IProductCommentRepository _productCommentRepository;
        IWishlistRepository _wishlistRepository;
        ICartRepository _cartRepository;
        IOrderRepository _orderRepository;
        IOrderDetailRepository _orderDetailRepository;
        ICouponRepository _couponRepository;
        ICouponConditionRepository _couponConditionRepository;
        ICouponOrderRepository _couponOrderRepository;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }
        public DbSet<T> Table<T>() where T : class => _context.Set<T>();

        // DI repository
        public IUserRepository UserRepository => _userRepository ??= new UserRepository(_context);
        public IRoleRepository RoleRepository => _roleRepository ??= new RoleRepository(_context);
        public IUserRoleRepository UserRoleRepository => _userRoleRepository ??= new UserRoleRepository(_context);
        public ITokenRepository TokenRepository => _tokenRepository ??= new TokenRepository(_context);
        public IParameterRepository ParameterRepository => _parameterRepository ??= new ParameterRepository(_context);
        public ILogRepository LogRepository => _logRepository ??= new LogRepository(_context);
        public IBannerRepository BannerRepository => _bannerRepository ??= new BannerRepository(_context);
        public ICategoryRepository CategoryRepository => _categoryRepository ??= new CategoryRepository(_context);
        public IRestaurantRepository RestaurantRepository => _restaurantRepository ??= new RestaurantRepository(_context);
        public IProductRepository ProductRepository => _productRepository ??= new ProductRepository(_context);
        public IProductDetailRepository ProductDetailRepository => _productDetailRepository ??= new ProductDetailRepository(_context);
        public IProductImageRepository ProductImageRepository => _productImageRepository ??= new ProductImageRepository(_context);
        public IProductCommentRepository ProductCommentRepository => _productCommentRepository ??= new ProductCommentRepository(_context);
        public IWishlistRepository WishlistRepository => _wishlistRepository ??= new WishlistRepository(_context);
        public ICartRepository CartRepository => _cartRepository ??= new CartRepository(_context);
        public IOrderRepository OrderRepository => _orderRepository ??= new OrderRepository(_context);
        public IOrderDetailRepository OrderDetailRepository => _orderDetailRepository ??= new OrderDetailRepository(_context);
        public ICouponRepository CouponRepository => _couponRepository ??= new CouponRepository(_context);
        public ICouponConditionRepository CouponConditionRepository => _couponConditionRepository ??= new CouponConditionRepository(_context);
        public ICouponOrderRepository CouponOrderRepository => _couponOrderRepository ??= new CouponOrderRepository(_context);
      

        public async Task BeginTransaction()
        {
            _dbContextTransaction = await _context.Database.BeginTransactionAsync();
        }
        public async Task CommitTransactionAsync()
        {
            await _dbContextTransaction.CommitAsync();
        }

        public async Task RollbackTransactionAsync()
        {
            await _dbContextTransaction.RollbackAsync();
        }

        public async Task SaveChangeAsync()
        {
            await _context.SaveChangesAsync();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _context.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
