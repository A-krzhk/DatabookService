namespace DatabookService.Application.DTOs
{
    //модель для сохранения результата валидации
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }

        public static ValidationResult Ok() => new ValidationResult { IsValid = true };
        public static ValidationResult Error(string message) => new ValidationResult { IsValid = false, ErrorMessage = message };
    }
}
