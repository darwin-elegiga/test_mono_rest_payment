UPDATE virtual_card.billing_addresses
SET line1       = '1 Optum Cir',
    city        = 'Eden Prairie',
    country     = 'MN',
    postal_code = '55344'
WHERE id = 1;

UPDATE virtual_card.cardholders
SET first_name         = 'Optum',
    last_name          = 'Financial',
    email              = 'payables@uhg.com',
    phone_number       = NULL
WHERE id = 1;