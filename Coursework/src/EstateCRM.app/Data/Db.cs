using System;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace EstateCRM.App.Data;

public static class Db
{
    public static EstateDbContext Create()
    {
        string filePath = Path.Combine(
            AppContext.BaseDirectory,
            "connection.txt");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Не найден connection.txt рядом с программой. " +
                "Проверь настройку копирования файла.",
                filePath);
        }

        string connectionString =
            File.ReadAllText(filePath).Trim();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Файл connection.txt пуст. " +
                "Укажите в нём строку подключения.");
        }

        var options =
            new DbContextOptionsBuilder<EstateDbContext>()
                .UseSqlServer(connectionString)
                .Options;

        return new EstateDbContext(options);
    }
}