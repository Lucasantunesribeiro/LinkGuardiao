import * as cdk from 'aws-cdk-lib';
import { Construct } from 'constructs';
import * as dynamodb from 'aws-cdk-lib/aws-dynamodb';
import * as lambda from 'aws-cdk-lib/aws-lambda';
import * as logs from 'aws-cdk-lib/aws-logs';
import * as path from 'path';
import * as fs from 'fs';

interface LinkGuardiaoStackProps extends cdk.StackProps {
  envName: string;
  corsAllowedOrigin?: string;
}

export class LinkGuardiaoStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props: LinkGuardiaoStackProps) {
    super(scope, id, props);

    const envName = props.envName;
    const isProd = envName === 'prod';
    const corsAllowedOrigin = props.corsAllowedOrigin ?? 'https://linkguardiao.pages.dev';

    const linksTable = new dynamodb.Table(this, 'LinksTable', {
      tableName: `linkguardiao-links-${envName}`,
      partitionKey: { name: 'shortCode', type: dynamodb.AttributeType.STRING },
      billingMode: dynamodb.BillingMode.PAY_PER_REQUEST,
      timeToLiveAttribute: 'expiresAtEpoch',
      removalPolicy: isProd ? cdk.RemovalPolicy.RETAIN : cdk.RemovalPolicy.DESTROY,
    });

    linksTable.addGlobalSecondaryIndex({
      indexName: 'gsi1',
      partitionKey: { name: 'userId', type: dynamodb.AttributeType.STRING },
      sortKey: { name: 'createdAt', type: dynamodb.AttributeType.STRING },
      projectionType: dynamodb.ProjectionType.ALL,
    });

    const usersTable = new dynamodb.Table(this, 'UsersTable', {
      tableName: `linkguardiao-users-${envName}`,
      partitionKey: { name: 'userId', type: dynamodb.AttributeType.STRING },
      billingMode: dynamodb.BillingMode.PAY_PER_REQUEST,
      removalPolicy: isProd ? cdk.RemovalPolicy.RETAIN : cdk.RemovalPolicy.DESTROY,
    });

    usersTable.addGlobalSecondaryIndex({
      indexName: 'gsi1',
      partitionKey: { name: 'email', type: dynamodb.AttributeType.STRING },
      projectionType: dynamodb.ProjectionType.ALL,
    });

    const accessTable = new dynamodb.Table(this, 'AccessTable', {
      tableName: `linkguardiao-access-${envName}`,
      partitionKey: { name: 'shortCode', type: dynamodb.AttributeType.STRING },
      sortKey: { name: 'accessTime', type: dynamodb.AttributeType.STRING },
      billingMode: dynamodb.BillingMode.PAY_PER_REQUEST,
      timeToLiveAttribute: 'expiresAtEpoch',
      removalPolicy: isProd ? cdk.RemovalPolicy.RETAIN : cdk.RemovalPolicy.DESTROY,
    });

    const dailyLimitsTable = new dynamodb.Table(this, 'DailyLimitsTable', {
      tableName: `linkguardiao-limits-${envName}`,
      partitionKey: { name: 'key', type: dynamodb.AttributeType.STRING },
      billingMode: dynamodb.BillingMode.PAY_PER_REQUEST,
      timeToLiveAttribute: 'expiresAtEpoch',
      removalPolicy: isProd ? cdk.RemovalPolicy.RETAIN : cdk.RemovalPolicy.DESTROY,
    });

    const lambdaCodePath = path.join(__dirname, '../../../artifacts/lambda');
    if (!fs.existsSync(lambdaCodePath)) {
      throw new Error(`Lambda artifact not found at ${lambdaCodePath}. Run scripts/aws/build-lambda first.`);
    }

    const jwtSecretParam = new cdk.CfnParameter(this, 'JwtSecret', {
      type: 'String',
      noEcho: true,
      minLength: 32,
    });

    const functionName = `linkguardiao-api-${envName}`;
    const apiLogGroup = new logs.LogGroup(this, 'ApiLogGroup', {
      logGroupName: `/aws/lambda/${functionName}`,
      retention: logs.RetentionDays.ONE_WEEK,
      removalPolicy: isProd ? cdk.RemovalPolicy.RETAIN : cdk.RemovalPolicy.DESTROY,
    });

    const apiFunction = new lambda.Function(this, 'ApiFunction', {
      functionName,
      runtime: lambda.Runtime.DOTNET_8,
      handler: 'LinkGuardiao.Api::LinkGuardiao.Api.LambdaEntryPoint::FunctionHandlerAsync',
      code: lambda.Code.fromAsset(lambdaCodePath),
      memorySize: 512,
      timeout: cdk.Duration.seconds(10),
      logGroup: apiLogGroup,
      environment: {
        DDB_TABLE_LINKS: linksTable.tableName,
        DDB_TABLE_USERS: usersTable.tableName,
        DDB_TABLE_ACCESS: accessTable.tableName,
        DDB_TABLE_DAILY_LIMITS: dailyLimitsTable.tableName,
        JWT__SECRET: jwtSecretParam.valueAsString,
        JWT__ISSUER: 'LinkGuardiao',
        JWT__AUDIENCE: 'LinkGuardiao',
        JWT__ACCESSTOKENMINUTES: '60',
        CORS_ALLOWED_ORIGIN: corsAllowedOrigin,
        LINKLIMITS__DAILYUSERCREATELIMIT: '100',
      },
    });

    linksTable.grantReadWriteData(apiFunction);
    usersTable.grantReadWriteData(apiFunction);
    accessTable.grantReadWriteData(apiFunction);
    dailyLimitsTable.grantReadWriteData(apiFunction);

    const functionUrl = apiFunction.addFunctionUrl({
      authType: lambda.FunctionUrlAuthType.NONE,
      cors: {
        allowedOrigins: [corsAllowedOrigin],
        allowedMethods: [
          lambda.HttpMethod.GET,
          lambda.HttpMethod.POST,
          lambda.HttpMethod.PUT,
          lambda.HttpMethod.DELETE,
        ],
        allowedHeaders: ['Content-Type', 'Authorization', 'Accept', 'X-Request-Id'],
        maxAge: cdk.Duration.hours(1),
      },
    });

    new cdk.CfnOutput(this, 'FunctionUrl', {
      value: functionUrl.url,
    });
  }
}
