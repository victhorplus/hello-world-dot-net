using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ITaskService>(new InMemoryTaskService());

var app = builder.Build();

var todos = new List<Todo>();

app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));
app.Use(async (context, next) => {
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Started.");
    next(context);
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Finished.");
});

app.MapGet("/", () => TypedResults.Ok("Hello World!"));

app.MapGet("/todos", (ITaskService service) => TypedResults.Ok(service.GetTodos()));

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id, ITaskService service) => {
    Todo targetTask = service.GetTodoById(id);
    return targetTask is null
        ? TypedResults.NotFound()
        : TypedResults.Ok(targetTask);
});

app.MapPost("/todos", (Todo task, ITaskService service) => {
    service.AddTodo(task);
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

app.MapDelete("/todos/{id}", (int id, ITaskService service) => {
    service.DeleteTodoById(id);
    return TypedResults.NoContent();
});

app.Run();

public record Todo(int id, string name, DateTime dueDate, bool isCompleted);

public interface ITaskService {
    List<Todo> GetTodos();
    Todo? GetTodoById(int id);
    void DeleteTodoById(int id);
    Todo AddTodo(Todo task);
}

public class InMemoryTaskService : ITaskService
{
    List<Todo> todos = new List<Todo>();

    public Todo AddTodo(Todo task) {
        todos.Add(task);
        return task;
    }

    public void DeleteTodoById(int id) {
        todos.RemoveAll(task => task.id == id);
    }

    public Todo? GetTodoById(int id) {
        return todos.SingleOrDefault(todo => todo.id == id);
    }

    public List<Todo> GetTodos() {
        return todos;
    }
}