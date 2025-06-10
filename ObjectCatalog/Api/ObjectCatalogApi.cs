using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ObjectCatalog.Models;
using ObjectCatalog.Services;

namespace ObjectCatalog.Api;

public class ObjectCatalogApi : IApi
{
    /// <summary>
    /// "/api/objects"
    /// </summary>
    public static string GetObjectsEP => "/api/objects";

    /// <summary>
    /// "/api/objects/{id}"
    /// </summary>
    public static string GetObjectByIdEP => "/api/objects/{id}";

    /// <summary>
    /// "/api/objects"
    /// </summary>
    public static string CreateObjectEP => "/api/objects";

    /// <summary>
    /// "/api/objects/{id}"
    /// </summary>
    public static string UpdateObjectEP => "/api/objects/{id}";

    /// <summary>
    /// "/api/objects/{id}"
    /// </summary>
    public static string DeleteObjectEP => "/api/objects/{id}";

    /// <summary>
    /// "/api/categories"
    /// </summary>
    public static string GetCategoriesEP => "/api/categories";

    public void Register(WebApplication app)
    {
        app.MapGet(GetObjectsEP, GetObjects)
            .Produces<PagedObjectResponseDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .WithName("GetObjects")
            .WithTags("Objects");

        app.MapGet(GetObjectByIdEP, GetObjectById)
            .Produces<ObjectDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .WithName("GetObjectById")
            .WithTags("Objects");

        app.MapPost(CreateObjectEP, CreateObject)
            .Produces<ObjectDto>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .WithName("CreateObject")
            .WithTags("Objects");

        app.MapPut(UpdateObjectEP, UpdateObject)
            .Produces<ObjectDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .WithName("UpdateObject")
            .WithTags("Objects");

        app.MapDelete(DeleteObjectEP, DeleteObject)
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .WithName("DeleteObject")
            .WithTags("Objects");

        app.MapGet(GetCategoriesEP, GetCategories)
            .Produces<List<CategoryDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .WithName("GetCategories")
            .WithTags("Categories");
    }

    private async Task<Results<Ok<PagedObjectResponseDto>, BadRequest<ProblemDetails>>> GetObjects(
        IObjectService service,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchQuery = null,
        [FromQuery] int? categoryId = null)
    {
        try
        {
            var (items, totalItems) = await service.GetPagedObjectsAsync(page * pageSize, pageSize, searchQuery, categoryId);

            var dtos = items.Select(o => new ObjectDto
            {
                Id = o.Id,
                Name = o.Name,
                Description = o.Description,
                Price = o.Price,
                CreatedDate = o.CreatedDate,
                Categories = o.Categories.Select(c => c.Name)
            }).ToList();

            return TypedResults.Ok(new PagedObjectResponseDto
            {
                Items = dtos,
                TotalItems = totalItems
            });
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Retrieval failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    private async Task<Results<Ok<ObjectDto>, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>> GetObjectById(
        IObjectService service,
        int id)
    {
        try
        {
            var obj = await service.GetObjectByIdAsync(id);

            if (obj == null)
            {
                return TypedResults.NotFound(new ProblemDetails
                {
                    Title = "Object not found",
                    Detail = $"No object found with ID {id}",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return TypedResults.Ok(new ObjectDto
            {
                Id = obj.Id,
                Name = obj.Name,
                Description = obj.Description,
                Price = obj.Price,
                CreatedDate = obj.CreatedDate,
                Categories = obj.Categories.Select(c => c.Name)
            });
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Retrieval failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    private async Task<Results<Created<ObjectDto>, BadRequest<ProblemDetails>>> CreateObject(
        IObjectService service,
        [FromBody] ObjectInputModel input)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input.Name) || input.Price < 0)
            {
                return TypedResults.BadRequest(new ProblemDetails
                {
                    Title = "Invalid input",
                    Detail = "Name and valid Price are required.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var obj = new ObjectEntity
            {
                Name = input.Name,
                Description = input.Description,
                Price = input.Price
            };

            await service.AddObjectAsync(obj, input.CategoryIds);

            return TypedResults.Created($"{CreateObjectEP}/{obj.Id}", new ObjectDto
            {
                Id = obj.Id,
                Name = obj.Name,
                Description = obj.Description,
                Price = obj.Price,
                CreatedDate = obj.CreatedDate,
                Categories = obj.Categories.Select(c => c.Name)
            });
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Operation failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    private async Task<Results<Ok<ObjectDto>, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>> UpdateObject(
        IObjectService service,
        int id,
        [FromBody] ObjectInputModel input)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input.Name) || input.Price < 0)
            {
                return TypedResults.BadRequest(new ProblemDetails
                {
                    Title = "Invalid input",
                    Detail = "Name and valid Price are required.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var obj = new ObjectEntity
            {
                Name = input.Name,
                Description = input.Description,
                Price = input.Price,                
            };

            await service.UpdateObjectAsync(id, obj, input.CategoryIds);

            return TypedResults.Ok(new ObjectDto
            {
                Id = id,
                Name = obj.Name,
                Description = obj.Description,
                Price = obj.Price,
                CreatedDate = obj.CreatedDate,
                Categories = []
            });
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Operation failed",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    private async Task<Results<Ok, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>> DeleteObject(
        IObjectService service,
        int id)
    {
        try
        {
            await service.DeleteObjectAsync(id);
            return TypedResults.Ok();
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Operation failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    private async Task<Results<Ok<List<CategoryDto>>, BadRequest<ProblemDetails>>> GetCategories(
        IObjectService service)
    {
        try
        {
            var categories = await service.GetAllCategoriesAsync();
            var dtos = categories.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name
            }).ToList();

            return TypedResults.Ok(dtos);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Retrieval failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }    
}