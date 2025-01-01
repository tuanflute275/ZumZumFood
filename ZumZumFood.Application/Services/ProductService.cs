﻿using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using X.PagedList;
using ZumZumFood.Application.Abstracts;
using ZumZumFood.Application.Models.DTOs;
using ZumZumFood.Application.Models.Request;
using ZumZumFood.Application.Models.Response;
using ZumZumFood.Application.Utils;
using ZumZumFood.Domain.Abstracts;
using ZumZumFood.Domain.Entities;
using static ZumZumFood.Application.Utils.Helpers;

namespace ZumZumFood.Application.Services
{
    public class ProductService : IProductService
    {
        IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public ProductService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ProductService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ResponseObject> GetAllPaginationAsync(string? keyword, string? sort, int pageNo = 1)
        {
            try
            {
                // validate invalid special characters
                var validationResult = InputValidator.ValidateInput(keyword, sort, pageNo);
                if (!string.IsNullOrEmpty(validationResult))
                {
                    LogHelper.LogWarning(_logger, "GET", $"/api/product", null, "Input contains invalid special characters");
                    return new ResponseObject(400, "Input contains invalid special characters", validationResult);
                }
                var dataQuery = _unitOfWork.ProductRepository.GetAllAsync(
                    expression: s => s.DeleteFlag == false && string.IsNullOrEmpty(keyword) || s.Name.Contains(keyword)
                );
                var query = await dataQuery;

                // Apply dynamic sorting based on the `sort` parameter
                if (!string.IsNullOrEmpty(sort))
                {
                    switch (sort)
                    {
                        case "Id-ASC":
                            query = query.OrderBy(x => x.ProductId);
                            break;
                        case "Id-DESC":
                            query = query.OrderByDescending(x => x.ProductId);
                            break;
                        case "Name-ASC":
                            query = query.OrderBy(x => x.Name);
                            break;
                        case "Name-DESC":
                            query = query.OrderByDescending(x => x.Name);
                            break;
                        case "Price-ASC":
                            query = query.OrderBy(x => x.Name);
                            break;
                        case "Price-DESC":
                            query = query.OrderByDescending(x => x.Price);
                            break;
                        default:
                            query = query.OrderByDescending(x => x.Price);
                            break;
                    }
                }

                // Map data to dataDTO
                var dataList = query.ToList();
                var data = _mapper.Map<List<ProductDTO>>(dataList);

                // Paginate the result
                // Phân trang dữ liệu
                var pagedData = data.ToPagedList(pageNo, Constant.DEFAULT_PAGESIZE);

                // Return the paginated result in the response
                // Trả về kết quả phân trang bao gồm các thông tin phân trang
                // Create paginated response
                var responseData = new
                {
                    items = pagedData,                // Paginated items
                    totalCount = pagedData.TotalItemCount, // Total number of items
                    totalPages = pagedData.PageCount,      // Total number of pages
                    pageNumber = pagedData.PageNumber,     // Current page number
                    pageSize = pagedData.PageSize          // Page size
                };
                LogHelper.LogInformation(_logger, "GET", "/api/product", null, pagedData.Count());
                return new ResponseObject(200, "Query data successfully", responseData);
            }
            catch (Exception ex)
            {
                LogHelper.LogError(_logger, ex, "GET", $"/api/product");
                return new ResponseObject(500, "Internal server error. Please try again later.", ex.Message);
            }
        }

        public async Task<ResponseObject> GetByIdAsync(int id)
        {
            try
            {
                // validate invalid special characters
                var validationResult = InputValidator.IsValidNumber(id);
                if (!validationResult)
                {
                    LogHelper.LogWarning(_logger, "GET", $"/api/product/{id}", null, "Invalid ID. ID must be greater than 0.");
                    return new ResponseObject(400, "Input invalid", "Invalid ID. ID must be greater than 0 and less than or equal to the maximum value of int!.");
                }
                var dataQuery = await _unitOfWork.ProductRepository.GetAllAsync(
                   expression: x => x.ProductId == id && x.DeleteFlag == false
                   /*,include: query => query.Include(x => x.Products).ThenInclude(p => p.ProductDetails)
                   .Include(x => x.Products).ThenInclude(p => p.ProductComments)
                   .Include(x => x.Products).ThenInclude(p => p.ProductImages)*/
                );
                var result = _mapper.Map<ProductDTO>(dataQuery.FirstOrDefault());
                if(result == null)
                {
                    LogHelper.LogWarning(_logger, "GET", "/api/product/{id}", null, result);
                    return new ResponseObject(404, "Product not found.", result);
                }
                LogHelper.LogInformation(_logger, "GET", "/api/product/{id}", null, result);
                return new ResponseObject(200, "Query data successfully", result);
            }
            catch (Exception ex)
            {
                LogHelper.LogError(_logger, ex, "GET", $"/api/product/{id}");
                return new ResponseObject(500, "Internal server error. Please try again later.", ex.Message);
            }
        }
        
        public async Task<ResponseObject> SaveAsync(ProductRequestModel model)
        {
            try
            {
                var request = _httpContextAccessor.HttpContext?.Request;
                // Validate data annotations
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(model, null, null);

                if (!Validator.TryValidateObject(model, validationContext, validationResults, true))
                {
                    var errorMessages = string.Join("; ", validationResults.Select(vr => vr.ErrorMessage));
                    return new ResponseObject(400, "Validation error", errorMessages);
                }
                // end validate

                // mapper data
                var product = new Product();
                product.Name = model.Name;
                product.Slug = Helpers.GenerateSlug(model.Name);
                product.Price = model.Price;
                product.Discount = model.Discount;
                product.IsActive = model.IsActive;
                product.RestaurantId = model.RestaurantId;
                product.CategoryId = model.CategoryId;
                product.Description = model.Description;
                product.CreateBy = Constant.SYSADMIN;
                product.CreateDate = DateTime.Now;
                if (model.ImageFile != null)
                {
                    var image = await FileUploadHelper.UploadImageAsync(model.ImageFile, model.OldImage, request.Scheme, request.Host.Value, "products");
                    product.Image = image;
                }

                await _unitOfWork.ProductRepository.SaveOrUpdateAsync(product);
                await _unitOfWork.SaveChangeAsync();
                LogHelper.LogInformation(_logger, "POST", "/api/product", model, product);
                return new ResponseObject(200, "Create data successfully", null);
            }
            catch (Exception ex)
            {
                LogHelper.LogError(_logger, ex, "POST", $"/api/product", model);
                return new ResponseObject(500, "Internal server error. Please try again later.", ex.Message);
            }
        }

        public async Task<ResponseObject> UpdateAsync(int id, ProductRequestModel model)
        {
            try
            {
                var request = _httpContextAccessor.HttpContext?.Request;
                // Validate data annotations
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(model, null, null);

                if (!Validator.TryValidateObject(model, validationContext, validationResults, true))
                {
                    var errorMessages = string.Join("; ", validationResults.Select(vr => vr.ErrorMessage));
                    return new ResponseObject(400, "Validation error", errorMessages);
                }
                // end validate

                // mapper data
                var product = await _unitOfWork.ProductRepository.GetByIdAsync(id);
                product.Name = model.Name;
                product.Slug = Helpers.GenerateSlug(model.Name);
                product.Price = model.Price;
                product.Discount = model.Discount;
                product.IsActive = model.IsActive;
                product.RestaurantId = model.RestaurantId;
                product.CategoryId = model.CategoryId;
                product.Description = model.Description;
                product.UpdateBy = Constant.SYSADMIN;
                product.UpdateDate = DateTime.Now;
                if (model.ImageFile != null)
                {
                    var image = await FileUploadHelper.UploadImageAsync(model.ImageFile, model.OldImage, request.Scheme, request.Host.Value, "products");
                    product.Image = image;
                }

                await _unitOfWork.ProductRepository.SaveOrUpdateAsync(product);
                await _unitOfWork.SaveChangeAsync();
                LogHelper.LogInformation(_logger, "POST", "/api/product", model, product);
                return new ResponseObject(200, "Update data successfully", null);
            }
            catch (Exception ex)
            {
                LogHelper.LogError(_logger, ex, "POST", $"/api/product", model);
                return new ResponseObject(500, "Internal server error. Please try again later.", ex.Message);
            }
        }

        public async Task<ResponseObject> DeleteFlagAsync(int id)
        {
            try
            {
                // validate invalid special characters
                var validationResult = InputValidator.IsValidNumber(id);
                if (!validationResult)
                {
                    LogHelper.LogWarning(_logger, "GET", $"/api/product/{id}", null, "Invalid ID. ID must be greater than 0.");
                    return new ResponseObject(400, "Input invalid", "Invalid ID. ID must be greater than 0 and less than or equal to the maximum value of int!.");
                }
                var product = await _unitOfWork.ProductRepository.GetByIdAsync(id);
                if (product == null)
                {
                    LogHelper.LogWarning(_logger, "POST", $"/api/product/{id}", id, "Product not found.");
                    return new ResponseObject(404, "Product not found.", null);
                }
                product.DeleteFlag = true;
                product.DeleteBy = Constant.SYSADMIN;
                product.DeleteDate = DateTime.Now;
                await _unitOfWork.ProductRepository.SaveOrUpdateAsync(product);
                await _unitOfWork.SaveChangeAsync();
                LogHelper.LogInformation(_logger, "POST", $"/api/product/{id}", id, "Deleted Flag successfully");
                return new ResponseObject(200, "Delete Flag data successfully", null);
            }
            catch (Exception ex)
            {
                LogHelper.LogError(_logger, ex, "PUT", $"/api/product/{id}", id);
                return new ResponseObject(500, "Internal server error. Please try again later.", ex.Message);
            }
        }

        public async Task<ResponseObject> GetDeletedListAsync()
        {
            try
            {
                var deletedData = await _unitOfWork.ProductRepository.GetAllAsync(x => x.DeleteFlag == true);
                if (deletedData == null || !deletedData.Any())
                {
                    LogHelper.LogWarning(_logger, "GET", "/api/deleted-data", null, new { message = "No deleted categories found." });
                    return new ResponseObject(404, "No deleted categories found.", null);
                }
                var data = _mapper.Map<List<ProductDTO>>(deletedData);
                LogHelper.LogInformation(_logger, "GET", $"/api/deleted-data", null, "Query data deleted successfully");
                return new ResponseObject(200, "Query data delete flag successfully.", data);
            }
            catch (Exception ex)
            {
                LogHelper.LogError(_logger, ex, "GET", "/api/product/deleted-data", null);
                return new ResponseObject(500, "Internal server error. Please try again later.", ex.Message);
            }
        }

        public async Task<ResponseObject> RestoreAsync(int id)
        {
            try
            {
                // validate invalid special characters
                var validationResult = InputValidator.IsValidNumber(id);
                if (!validationResult)
                {
                    LogHelper.LogWarning(_logger, "GET", $"/api/product/{id}", null, "Invalid ID. ID must be greater than 0.");
                    return new ResponseObject(400, "Input invalid", "Invalid ID. ID must be greater than 0 and less than or equal to the maximum value of int!.");
                }
                var product = await _unitOfWork.ProductRepository.GetByIdAsync(id);
                if (product == null)
                {
                    LogHelper.LogWarning(_logger, "POST", $"/api/product/{id}/restore", new { id }, new { message = "Product not found." });
                    return new ResponseObject(404, "Product not found.", null);
                }

                if ((bool)!product.DeleteFlag)
                {
                    LogHelper.LogWarning(_logger, "POST", $"/api/product/{id}/restore", new { id }, new { message = "Product is not flagged as deleted." });
                    return new ResponseObject(400, "Product is not flagged as deleted.", null);
                }

                product.DeleteFlag = false;
                product.DeleteBy = null;
                product.DeleteDate = null;

                await _unitOfWork.ProductRepository.SaveOrUpdateAsync(product);
                await _unitOfWork.SaveChangeAsync();

                LogHelper.LogInformation(_logger, "POST", $"/api/product/{id}/restore", id, "Product restored successfully");
                return new ResponseObject(200, "Product restored successfully.", null);
            }
            catch (Exception ex)
            {
                LogHelper.LogError(_logger, ex, "POST", $"/api/product/{id}/restore", id);
                return new ResponseObject(500, "Internal server error. Please try again later.", ex.Message);
            }
        }

        public async Task<ResponseObject> DeleteAsync(int id)
        {
            try
            {
                // validate invalid special characters
                var validationResult = InputValidator.IsValidNumber(id);
                if (!validationResult)
                {
                    LogHelper.LogWarning(_logger, "GET", $"/api/product/{id}", null, "Invalid ID. ID must be greater than 0.");
                    return new ResponseObject(400, "Input invalid", "Invalid ID. ID must be greater than 0 and less than or equal to the maximum value of int!.");
                }
                var product = await _unitOfWork.ProductRepository.GetByIdAsync(id);
                if (product == null)
                {
                    LogHelper.LogWarning(_logger, "DELETE", $"/api/product/{id}", id, "Product not found.");
                    return new ResponseObject(404, "Product not found.", null);
                }
                // Start: Deleting foreign key dependencies
                var productDetail = await _unitOfWork.ProductDetailRepository.GetAllAsync(x => x.ProductId == id);
                var productComment = await _unitOfWork.ProductCommentRepository.GetAllAsync(x => x.ProductId == id);
                var productImage = await _unitOfWork.ProductImageRepository.GetAllAsync(x => x.ProductId == id);

                // Delete Product Details
                if (productDetail != null && productDetail.Any())  // Ensure there's data to delete
                {
                    await _unitOfWork.ProductDetailRepository.DeleteRangeAsync(productDetail.ToList());
                }

                // Delete Product Comments
                if (productComment != null && productComment.Any())  // Ensure there's data to delete
                {
                    await _unitOfWork.ProductCommentRepository.DeleteRangeAsync(productComment.ToList());
                }

                // Delete Product Images
                if (productImage != null && productImage.Any())  // Ensure there's data to delete
                {
                    await _unitOfWork.ProductImageRepository.DeleteRangeAsync(productImage.ToList());
                }

                // Save changes after all deletions
                await _unitOfWork.SaveChangeAsync();
                // End: Deleting foreign key dependencies
                await _unitOfWork.ProductRepository.DeleteAsync(product);
                await _unitOfWork.SaveChangeAsync();
                LogHelper.LogInformation(_logger, "DELETE", $"/api/product/{id}", id, "Deleted successfully");
                return new ResponseObject(200, "Delete data successfully", null);
            }
            catch (Exception ex)
            {
                LogHelper.LogError(_logger, ex, "DELETE", $"/api/product/{id}", id);
                return new ResponseObject(500, "Internal server error. Please try again later.", ex.Message);
            }
        }
    }
}