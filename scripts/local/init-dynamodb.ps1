param(
    [string]$EndpointUrl = "http://localhost:8000"
)

$ErrorActionPreference = "Stop"

function Get-ExistingTables {
    $json = aws dynamodb list-tables --endpoint-url $EndpointUrl | Out-String
    return ((ConvertFrom-Json $json).TableNames)
}

function Ensure-Table {
    param(
        [string]$TableName,
        [string[]]$Arguments,
        [bool]$EnableTtl = $false
    )

    if ($script:ExistingTables -contains $TableName) {
        Write-Host "Table already exists: $TableName"
    }
    else {
        Write-Host "Creating table: $TableName"
        & aws dynamodb create-table --table-name $TableName @Arguments --endpoint-url $EndpointUrl | Out-Null
        & aws dynamodb wait table-exists --table-name $TableName --endpoint-url $EndpointUrl
        $script:ExistingTables += $TableName
    }

    if ($EnableTtl) {
        & aws dynamodb update-time-to-live `
            --table-name $TableName `
            --time-to-live-specification "Enabled=true, AttributeName=expiresAtEpoch" `
            --endpoint-url $EndpointUrl | Out-Null
    }
}

$script:ExistingTables = @(Get-ExistingTables)

Ensure-Table "linkguardiao-links-dev" @(
    "--attribute-definitions", "AttributeName=shortCode,AttributeType=S", "AttributeName=userId,AttributeType=S", "AttributeName=createdAt,AttributeType=S",
    "--key-schema", "AttributeName=shortCode,KeyType=HASH",
    "--billing-mode", "PAY_PER_REQUEST",
    "--global-secondary-indexes", '[{"IndexName":"gsi1","KeySchema":[{"AttributeName":"userId","KeyType":"HASH"},{"AttributeName":"createdAt","KeyType":"RANGE"}],"Projection":{"ProjectionType":"ALL"}}]'
) $true

Ensure-Table "linkguardiao-users-dev" @(
    "--attribute-definitions", "AttributeName=userId,AttributeType=S", "AttributeName=email,AttributeType=S",
    "--key-schema", "AttributeName=userId,KeyType=HASH",
    "--billing-mode", "PAY_PER_REQUEST",
    "--global-secondary-indexes", '[{"IndexName":"gsi1","KeySchema":[{"AttributeName":"email","KeyType":"HASH"}],"Projection":{"ProjectionType":"ALL"}}]'
)

Ensure-Table "linkguardiao-access-dev" @(
    "--attribute-definitions", "AttributeName=shortCode,AttributeType=S", "AttributeName=accessTime,AttributeType=S",
    "--key-schema", "AttributeName=shortCode,KeyType=HASH", "AttributeName=accessTime,KeyType=RANGE",
    "--billing-mode", "PAY_PER_REQUEST"
) $true

Ensure-Table "linkguardiao-limits-dev" @(
    "--attribute-definitions", "AttributeName=key,AttributeType=S",
    "--key-schema", "AttributeName=key,KeyType=HASH",
    "--billing-mode", "PAY_PER_REQUEST"
) $true

Ensure-Table "linkguardiao-refresh-tokens-dev" @(
    "--attribute-definitions", "AttributeName=tokenHash,AttributeType=S", "AttributeName=userId,AttributeType=S",
    "--key-schema", "AttributeName=tokenHash,KeyType=HASH",
    "--billing-mode", "PAY_PER_REQUEST",
    "--global-secondary-indexes", '[{"IndexName":"gsi1-userId","KeySchema":[{"AttributeName":"userId","KeyType":"HASH"}],"Projection":{"ProjectionType":"ALL"}}]'
) $true

Ensure-Table "linkguardiao-email-locks-dev" @(
    "--attribute-definitions", "AttributeName=email,AttributeType=S",
    "--key-schema", "AttributeName=email,KeyType=HASH",
    "--billing-mode", "PAY_PER_REQUEST"
)

Write-Host "DynamoDB Local bootstrap completed."
