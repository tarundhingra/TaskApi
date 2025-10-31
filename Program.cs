using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add required services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalDev", policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

var app = builder.Build();

// Enable Swagger & CORS
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowLocalDev");

// In-memory task list
var tasks = new List<TaskItem>();
var lockObj = new object();

// GET: Fetch all tasks
app.MapGet("/api/tasks", () =>
{
    lock (lockObj)
        return Results.Ok(tasks.ToList());
});

// POST: Add a new task
app.MapPost("/api/tasks", (TaskItem newTask) =>
{
    if (string.IsNullOrWhiteSpace(newTask.Description))
        return Results.BadRequest(new { message = "Description is required." });

    var task = new TaskItem { Description = newTask.Description.Trim() };
    lock (lockObj) tasks.Add(task);
    return Results.Created($"/api/tasks/{task.Id}", task);
});

// PUT: Update task
app.MapPut("/api/tasks/{id:guid}", (Guid id, TaskItem updated) =>
{
    lock (lockObj)
    {
        var existing = tasks.FirstOrDefault(t => t.Id == id);
        if (existing == null) return Results.NotFound();

        existing.Description = updated.Description ?? existing.Description;
        existing.IsCompleted = updated.IsCompleted;
        return Results.Ok(existing);
    }
});

// DELETE: Remove a task
app.MapDelete("/api/tasks/{id:guid}", (Guid id) =>
{
    lock (lockObj)
    {
        var existing = tasks.FirstOrDefault(t => t.Id == id);
        if (existing == null) return Results.NotFound();

        tasks.Remove(existing);
        return Results.NoContent();
    }
});

app.Run();

// ✅ Keep class at the very bottom (after all top-level statements)
public class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = false;
}
