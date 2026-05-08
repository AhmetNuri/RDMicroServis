<?php
return [
    'secret' => env('JWT_SECRET'),
    'issuer' => env('JWT_ISSUER', 'auth-service'),
    'audience' => env('JWT_AUDIENCE', 'commerce-platform'),
];
