CREATE TABLE public.orders (
    id SERIAL PRIMARY KEY,
    order_number INTEGER NOT NULL DEFAULT nextval('public.order_number_seq'),
    user_id INTEGER NOT NULL REFERENCES public.users(id),
    status public.order_status NOT NULL DEFAULT 'pending',
    total_amount NUMERIC(10, 2) NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);
