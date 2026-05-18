CREATE TABLE sales_summary (
    id SERIAL PRIMARY KEY,
    customer_id INTEGER NOT NULL,
    total_sales NUMERIC(12,2) NOT NULL DEFAULT 0,
    report_date DATE NOT NULL DEFAULT CURRENT_DATE
);
