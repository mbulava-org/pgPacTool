CREATE OR REPLACE VIEW public.active_orders AS
SELECT
    o.id,
    o.order_number,
    o.user_id,
    o.status,
    o.total_amount,
    o.created_at,
    o.updated_at
FROM public.orders o
WHERE o.status NOT IN ('delivered', 'cancelled');
