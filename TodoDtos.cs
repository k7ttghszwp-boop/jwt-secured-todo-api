namespace TodoApp.Data
{
    // Listeleme ve tekil getirme için dışarıya döneceğimiz model
    public record TodoDto(int Id, string Title, bool IsCompleted);

    // POST için sadece gerekli alanlar
    public record CreateTodoDto(string Title);

    // PUT için güncellenecek alanlar
    public record UpdateTodoDto(string Title, bool IsCompleted);
}