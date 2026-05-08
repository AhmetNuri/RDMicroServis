<?php
return [
    'default' => env('DB_CONNECTION', 'pgsql'),
    'connections' => [
        'pgsql' => [
            'driver' => 'pgsql',
            'host' => env('DB_HOST', 'postgres-payment'),
            'port' => env('DB_PORT', '5432'),
            'database' => env('DB_DATABASE', 'paymentdb'),
            'username' => env('DB_USERNAME', 'paymentuser'),
            'password' => env('DB_PASSWORD', ''),
            'charset' => 'utf8',
            'prefix' => '',
            'schema' => 'public',
        ],
    ],
    'migrations' => 'migrations',
];
