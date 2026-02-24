CREATE OR REPLACE FUNCTION calculate_order_total(p_order_id INTEGER)
RETURNS DECIMAL(10,2)
LANGUAGE SQL
STABLE
AS $$
    SELECT COALESCE(SUM(subtotal), 0.00)
    FROM order_items
    WHERE order_id = p_order_id;
$$;
