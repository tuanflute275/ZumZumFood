﻿namespace ZumZumFood.Application.Abstracts
{
    public interface IBannerService
    {
        Task<ResponseObject> GetAllPaginationAsync(string? keyword, string? sort, int pageNo = 1);
        Task<ResponseObject> GetByIdAsync(int id);
        Task<ResponseObject> SaveAsync(BannerModel model);
        Task<ResponseObject> UpdateAsync(int id, BannerModel model);
        Task<ResponseObject> DeleteAsync(int id);
        Task<ResponseObject> DeleteFlagAsync(int id);
        Task<ResponseObject> GetDeletedListAsync();
        Task<ResponseObject> RestoreAsync(int id);
    }
}
