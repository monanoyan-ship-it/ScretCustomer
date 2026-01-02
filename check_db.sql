SELECT a."Id", a."AssignedUserId", u."Username", a."DueDate", a."IsDeleted" FROM "Assignments" a LEFT JOIN "Users" u ON a."AssignedUserId" = u."Id" LIMIT 10;
