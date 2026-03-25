-- Pagila Sample Database - Simplified Version for Testing
-- Based on the Sakila database (MySQL) adapted for PostgreSQL

-- Create schema
CREATE SCHEMA IF NOT EXISTS public;

-- Country table
CREATE TABLE country (
    country_id SERIAL PRIMARY KEY,
    country VARCHAR(50) NOT NULL,
    last_update TIMESTAMP NOT NULL DEFAULT NOW()
);

-- City table
CREATE TABLE city (
    city_id SERIAL PRIMARY KEY,
    city VARCHAR(50) NOT NULL,
    country_id INT NOT NULL,
    last_update TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_city_country FOREIGN KEY (country_id) REFERENCES country(country_id) ON DELETE RESTRICT ON UPDATE CASCADE
);

-- Address table
CREATE TABLE address (
    address_id SERIAL PRIMARY KEY,
    address VARCHAR(50) NOT NULL,
    address2 VARCHAR(50),
    district VARCHAR(20) NOT NULL,
    city_id INT NOT NULL,
    postal_code VARCHAR(10),
    phone VARCHAR(20) NOT NULL,
    last_update TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_address_city FOREIGN KEY (city_id) REFERENCES city(city_id) ON DELETE RESTRICT ON UPDATE CASCADE
);

-- Actor table
CREATE TABLE actor (
    actor_id SERIAL PRIMARY KEY,
    first_name VARCHAR(45) NOT NULL,
    last_name VARCHAR(45) NOT NULL,
    last_update TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Category table
CREATE TABLE category (
    category_id SERIAL PRIMARY KEY,
    name VARCHAR(25) NOT NULL,
    last_update TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Film table
CREATE TABLE film (
    film_id SERIAL PRIMARY KEY,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    release_year INTEGER,
    language_id INT NOT NULL,
    rental_duration SMALLINT NOT NULL DEFAULT 3,
    rental_rate NUMERIC(4, 2) NOT NULL DEFAULT 4.99,
    length SMALLINT,
    replacement_cost NUMERIC(5, 2) NOT NULL DEFAULT 19.99,
    rating VARCHAR(10) DEFAULT 'G',
    last_update TIMESTAMP NOT NULL DEFAULT NOW(),
    special_features TEXT[]
);

-- Film_actor junction table
CREATE TABLE film_actor (
    actor_id INT NOT NULL,
    film_id INT NOT NULL,
    last_update TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_film_actor PRIMARY KEY (actor_id, film_id),
    CONSTRAINT fk_film_actor_actor FOREIGN KEY (actor_id) REFERENCES actor(actor_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_film_actor_film FOREIGN KEY (film_id) REFERENCES film(film_id) ON DELETE RESTRICT ON UPDATE CASCADE
);

-- Film_category junction table
CREATE TABLE film_category (
    film_id INT NOT NULL,
    category_id INT NOT NULL,
    last_update TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_film_category PRIMARY KEY (film_id, category_id),
    CONSTRAINT fk_film_category_film FOREIGN KEY (film_id) REFERENCES film(film_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_film_category_category FOREIGN KEY (category_id) REFERENCES category(category_id) ON DELETE RESTRICT ON UPDATE CASCADE
);

-- Store table
CREATE TABLE store (
    store_id SERIAL PRIMARY KEY,
    manager_staff_id INT NOT NULL,
    address_id INT NOT NULL,
    last_update TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Staff table
CREATE TABLE staff (
    staff_id SERIAL PRIMARY KEY,
    first_name VARCHAR(45) NOT NULL,
    last_name VARCHAR(45) NOT NULL,
    address_id INT NOT NULL,
    email VARCHAR(50),
    store_id INT NOT NULL,
    active BOOLEAN NOT NULL DEFAULT TRUE,
    username VARCHAR(16) NOT NULL,
    password VARCHAR(40),
    last_update TIMESTAMP NOT NULL DEFAULT NOW(),
    picture BYTEA,
    CONSTRAINT fk_staff_address FOREIGN KEY (address_id) REFERENCES address(address_id) ON DELETE RESTRICT ON UPDATE CASCADE
);

-- Add FK from store to staff (circular reference handled by deferred constraint)
ALTER TABLE store 
    ADD CONSTRAINT fk_store_staff FOREIGN KEY (manager_staff_id) REFERENCES staff(staff_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    ADD CONSTRAINT fk_store_address FOREIGN KEY (address_id) REFERENCES address(address_id) ON DELETE RESTRICT ON UPDATE CASCADE;

ALTER TABLE staff
    ADD CONSTRAINT fk_staff_store FOREIGN KEY (store_id) REFERENCES store(store_id) ON DELETE RESTRICT ON UPDATE CASCADE;

-- Customer table
CREATE TABLE customer (
    customer_id SERIAL PRIMARY KEY,
    store_id INT NOT NULL,
    first_name VARCHAR(45) NOT NULL,
    last_name VARCHAR(45) NOT NULL,
    email VARCHAR(50),
    address_id INT NOT NULL,
    active BOOLEAN NOT NULL DEFAULT TRUE,
    create_date DATE NOT NULL DEFAULT CURRENT_DATE,
    last_update TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_customer_address FOREIGN KEY (address_id) REFERENCES address(address_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_customer_store FOREIGN KEY (store_id) REFERENCES store(store_id) ON DELETE RESTRICT ON UPDATE CASCADE
);

-- Inventory table
CREATE TABLE inventory (
    inventory_id SERIAL PRIMARY KEY,
    film_id INT NOT NULL,
    store_id INT NOT NULL,
    last_update TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_inventory_film FOREIGN KEY (film_id) REFERENCES film(film_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_inventory_store FOREIGN KEY (store_id) REFERENCES store(store_id) ON DELETE RESTRICT ON UPDATE CASCADE
);

-- Rental table
CREATE TABLE rental (
    rental_id SERIAL PRIMARY KEY,
    rental_date TIMESTAMP NOT NULL,
    inventory_id INT NOT NULL,
    customer_id INT NOT NULL,
    return_date TIMESTAMP,
    staff_id INT NOT NULL,
    last_update TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_rental_inventory FOREIGN KEY (inventory_id) REFERENCES inventory(inventory_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_rental_customer FOREIGN KEY (customer_id) REFERENCES customer(customer_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_rental_staff FOREIGN KEY (staff_id) REFERENCES staff(staff_id) ON DELETE RESTRICT ON UPDATE CASCADE
);

-- Payment table
CREATE TABLE payment (
    payment_id SERIAL PRIMARY KEY,
    customer_id INT NOT NULL,
    staff_id INT NOT NULL,
    rental_id INT NOT NULL,
    amount NUMERIC(5, 2) NOT NULL,
    payment_date TIMESTAMP NOT NULL,
    CONSTRAINT fk_payment_customer FOREIGN KEY (customer_id) REFERENCES customer(customer_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_payment_staff FOREIGN KEY (staff_id) REFERENCES staff(staff_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_payment_rental FOREIGN KEY (rental_id) REFERENCES rental(rental_id) ON DELETE SET NULL ON UPDATE CASCADE
);

-- Create indexes for foreign keys
CREATE INDEX idx_fk_city_country ON city(country_id);
CREATE INDEX idx_fk_address_city ON address(city_id);
CREATE INDEX idx_fk_film_actor_actor ON film_actor(actor_id);
CREATE INDEX idx_fk_film_actor_film ON film_actor(film_id);
CREATE INDEX idx_fk_film_category_film ON film_category(film_id);
CREATE INDEX idx_fk_film_category_category ON film_category(category_id);
CREATE INDEX idx_fk_staff_address ON staff(address_id);
CREATE INDEX idx_fk_staff_store ON staff(store_id);
CREATE INDEX idx_fk_customer_address ON customer(address_id);
CREATE INDEX idx_fk_customer_store ON customer(store_id);
CREATE INDEX idx_fk_inventory_film ON inventory(film_id);
CREATE INDEX idx_fk_inventory_store ON inventory(store_id);
CREATE INDEX idx_fk_rental_inventory ON rental(inventory_id);
CREATE INDEX idx_fk_rental_customer ON rental(customer_id);
CREATE INDEX idx_fk_rental_staff ON rental(staff_id);
CREATE INDEX idx_fk_payment_customer ON payment(customer_id);
CREATE INDEX idx_fk_payment_staff ON payment(staff_id);
CREATE INDEX idx_fk_payment_rental ON payment(rental_id);

-- Create a view
CREATE OR REPLACE VIEW actor_info AS
SELECT 
    a.actor_id,
    a.first_name,
    a.last_name,
    COUNT(fa.film_id) AS film_count
FROM actor a
LEFT JOIN film_actor fa ON a.actor_id = fa.actor_id
GROUP BY a.actor_id, a.first_name, a.last_name;

-- Create a function
CREATE OR REPLACE FUNCTION get_customer_balance(p_customer_id INTEGER, p_effective_date TIMESTAMP)
RETURNS NUMERIC AS $$
DECLARE
    v_balance NUMERIC;
BEGIN
    SELECT COALESCE(SUM(amount), 0)
    INTO v_balance
    FROM payment
    WHERE customer_id = p_customer_id
    AND payment_date <= p_effective_date;
    
    RETURN v_balance;
END;
$$ LANGUAGE plpgsql;

-- Insert some sample data
INSERT INTO country (country) VALUES ('United States'), ('Canada'), ('Mexico');
INSERT INTO city (city, country_id) VALUES ('Los Angeles', 1), ('Toronto', 2), ('Mexico City', 3);
INSERT INTO address (address, district, city_id, phone) VALUES 
    ('123 Main St', 'CA', 1, '555-1234'),
    ('456 Elm St', 'ON', 2, '555-5678');
INSERT INTO actor (first_name, last_name) VALUES ('John', 'Doe'), ('Jane', 'Smith');
INSERT INTO category (name) VALUES ('Action'), ('Comedy'), ('Drama');
