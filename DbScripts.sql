CREATE TABLE Documents (
    Id SERIAL PRIMARY KEY,
    TraceId UUID NOT NULL,
    EmployeeId VARCHAR(20) NOT NULL,
    DocumentId VARCHAR(255) NOT NULL,
    Content BYTEA NOT NULL
);