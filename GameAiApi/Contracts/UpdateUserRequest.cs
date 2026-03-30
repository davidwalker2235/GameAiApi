namespace GameAiApi.Contracts;

public sealed record UpdateUserRequest(string Name, string Email, int Points);
