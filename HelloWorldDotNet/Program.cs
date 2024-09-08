using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var todos = new List<Todo>();

app.MapGet("/", (HttpContext req) => {

    return TypedResults.Ok("Hello World!");
});

app.MapGet("/todos", () => todos);

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id) => {
    Todo targetTask = todos.SingleOrDefault(task => task.id == id);
    return targetTask is null
        ? TypedResults.NotFound()
        : TypedResults.Ok(targetTask);
});

app.MapPost("/todos", (Todo task) => {
    todos.Add(task);
    return TypedResults.Created($"/todos/{task.id}", task);
});

app.MapDelete("todos/{id}", (int id) => {
    todos.RemoveAll(task => task.id == id);
    return TypedResults.NoContent();
});

app.Run();

public record Todo(int id, string name, DateTime dueDate, bool isCompleted);