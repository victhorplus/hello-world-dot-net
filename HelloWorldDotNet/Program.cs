using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var todos = new List<Todo>();

app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));
app.Use(async (context, next) => {
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Started.");
    next(context);
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Finished.");
});

app.MapGet("/", () => TypedResults.Ok("Hello World!"));

app.MapGet("/todos", () => TypedResults.Ok(todos));

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id) => {
    Todo targetTask = todos.SingleOrDefault(task => task.id == id);
    return targetTask is null
        ? TypedResults.NotFound()
        : TypedResults.Ok(targetTask);
});

app.MapPost("/todos", (Todo task) => {
    todos.Add(task);
    return TypedResults.Created($"/todos/{task.id}", task);
})
.AddEndpointFilter(async (EndpointFilterInvocationContext context, EndpointFilterDelegate next) => {
    Todo task = context.GetArgument<Todo>(0);
    var errors = new Dictionary<string, string[]>();

    if(task.dueDate < DateTime.UtcNow){
        errors.Add(nameof(Todo.dueDate), ["Cannot have due date in the past."]);
    }
    if(task.isCompleted){
        errors.Add(nameof(Todo.isCompleted), ["Canno add completed todo."]);
    }

    return errors.Count > 0
        ? Results.ValidationProblem(errors)
        : next(context);
});

app.MapDelete("/todos/{id}", (int id) => {
    todos.RemoveAll(task => task.id == id);
    return TypedResults.NoContent();
});

app.Run();

public record Todo(int id, string name, DateTime dueDate, bool isCompleted);