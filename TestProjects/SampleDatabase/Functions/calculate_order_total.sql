CREATE OR REPLACE FUNCTION public.calculate_order_total(p_order_id INTEGER)
RETURNS NUMERIC(10, 2) AS $$
DECLARE
    v_total NUMERIC(10, 2);
BEGIN
    SELECT COALESCE(SUM(subtotal), 0)
    INTO v_total
    FROM public.order_items
    WHERE order_id = p_order_id;

    RETURN v_total;
END;
$$ LANGUAGE plpgsql;
