CREATE OR REPLACE VIEW active_orders AS
SELECT 
    o.id,
    o.user_id,
    u.username,
    u.email,
    o.status,
    o.total_amount,
    o.order_date,
    COUNT(oi.id) as item_count
FROM orders o
JOIN users u ON o.user_id = u.id
LEFT JOIN order_items oi ON o.id = oi.order_id
WHERE o.status IN ('pending', 'processing', 'shipped')
GROUP BY o.id, o.user_id, u.username, u.email, o.status, o.total_amount, o.order_date;
