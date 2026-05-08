<?php
return [
    'default' => env('DB_CONNECTION', 'mongodb'),
    'connections' => [
        'mongodb' => [
            'driver'   => 'mongodb',
            'host'     => env('MONGO_PRODUCT_HOST', '127.0.0.1'),
            'port'     => env('MONGO_PRODUCT_PORT', 27017),
            'database' => env('MONGO_PRODUCT_DB', 'productdb'),
            'username' => env('MONGO_PRODUCT_USER', ''),
            'password' => env('MONGO_PRODUCT_PASSWORD', ''),
            'options'  => [
                'appname' => 'product-service',
            ],
        ],
        'sqlite' => [
            'driver' => 'sqlite',
            'database' => env('DB_DATABASE', database_path('database.sqlite')),
            'prefix' => '',
            'foreign_key_constraints' => env('DB_FOREIGN_KEYS', true),
        ],
    ],
    'migrations' => 'migrations',
    'redis' => [
        'client' => env('REDIS_CLIENT', 'predis'),
        'default' => [
            'host'     => env('REDIS_HOST', '127.0.0.1'),
            'password' => env('REDIS_PASSWORD', null),
            'port'     => env('REDIS_PORT', 6379),
            'database' => env('REDIS_DB', 0),
        ],
        'cache' => [
            'host'     => env('REDIS_HOST', '127.0.0.1'),
            'password' => env('REDIS_PASSWORD', null),
            'port'     => env('REDIS_PORT', 6379),
            'database' => env('REDIS_CACHE_DB', 1),
        ],
    ],
];
