namespace GameAiApi.Contracts;

public sealed record CreateUserRequest(string Name, string Email, int Points);
