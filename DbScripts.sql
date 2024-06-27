CREATE TABLE Documents (
    Id SERIAL PRIMARY KEY,
    TraceId UUID NOT NULL,
    EmployeeId VARCHAR(20) NOT NULL,
    DocumentId VARCHAR(255) NOT NULL,
    InsertTime TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    Content BYTEA NOT NULL
);