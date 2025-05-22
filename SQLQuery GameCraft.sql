INSERT INTO Categories (Name) VALUES ('Electronics');
INSERT INTO Categories (Name) VALUES ('Clothing');
INSERT INTO Categories (Name) VALUES ('Books');

INSERT INTO Customers (FirstName, LastName, Email, Phone, Address) 
VALUES ('John', 'Doe', 'john.doe@example.com', '123-456-7890', '123 Main St, Anytown, USA');

INSERT INTO Customers (FirstName, LastName, Email, Phone, Address) 
VALUES ('Jane', 'Smith', 'jane.smith@example.com', '098-765-4321', '456 Elm St, Othertown, USA');

INSERT INTO Products (Name, Description, Price, CategoryId) 
VALUES ('Smartphone', 'Latest model smartphone with advanced features', 699.99, 1);

INSERT INTO Products (Name, Description, Price, CategoryId) 
VALUES ('T-Shirt', '100% cotton t-shirt', 19.99, 2);

INSERT INTO Products (Name, Description, Price, CategoryId) 
VALUES ('Programming Book', 'Learn SQL Server with this comprehensive guide', 29.99, 3);

INSERT INTO Orders (CustomerId, TotalAmount) 
VALUES (1, 719.98);  -- Assuming CustomerId 1 is John Doe

INSERT INTO Orders (CustomerId, TotalAmount) 
VALUES (2, 49.98);   -- Assuming CustomerId 2 is Jane Smith

INSERT INTO OrderDetails (OrderId, ProductId, Quantity, UnitPrice) 
VALUES (1, 1, 1, 699.99);  -- OrderId 1, Smartphone

INSERT INTO OrderDetails (OrderId, ProductId, Quantity, UnitPrice) 
VALUES (1, 3, 1, 29.99);   -- OrderId 1, Programming Book

INSERT INTO OrderDetails (OrderId, ProductId, Quantity, UnitPrice) 
VALUES (2, 2, 2, 19.99);   -- OrderId 2, T-Shirt (2 items)

SELECT * FROM Categories;
SELECT * FROM Customers;
SELECT * FROM Products;
SELECT * FROM Orders;
SELECT * FROM OrderDetails;