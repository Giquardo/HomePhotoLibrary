using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace PhotoAlbumApi.Swagger;

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParams = context.MethodInfo.GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile))
            .ToList();

        if (fileParams.Any())
        {
            operation.Parameters.Clear();
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                ["file"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary",
                                    Description = "File must be provided."
                                },
                                ["albumId"] = new OpenApiSchema
                                {
                                    Type = "integer",
                                    Format = "int32"
                                },
                                ["title"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    MaxLength = 100
                                },
                                ["description"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    MaxLength = 500
                                }
                            },
                            Required = new HashSet<string> { "file", "albumId", "title" },
                            Description = "File must be provided."
                        }
                    }
                }
            };
        }
    }
}