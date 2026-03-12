import * as cdk from 'aws-cdk-lib';
import { Construct } from 'constructs';
import * as dynamodb from 'aws-cdk-lib/aws-dynamodb';
import * as lambda from 'aws-cdk-lib/aws-lambda';
import * as lambda_event_sources from 'aws-cdk-lib/aws-lambda-event-sources';
import * as sqs from 'aws-cdk-lib/aws-sqs';
import * as apigwv2 from 'aws-cdk-lib/aws-apigatewayv2';
import * as apigwv2_integrations from 'aws-cdk-lib/aws-apigatewayv2-integrations';
import * as wafv2 from 'aws-cdk-lib/aws-wafv2';
import * as logs from 'aws-cdk-lib/aws-logs';
import * as iam from 'aws-cdk-lib/aws-iam';
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

    // ─── DynamoDB Tables ─────────────────────────────────────────────────────

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

    const refreshTokensTable = new dynamodb.Table(this, 'RefreshTokensTable', {
      tableName: `linkguardiao-refresh-tokens-${envName}`,
      partitionKey: { name: 'tokenHash', type: dynamodb.AttributeType.STRING },
      billingMode: dynamodb.BillingMode.PAY_PER_REQUEST,
      timeToLiveAttribute: 'expiresAtEpoch',
      removalPolicy: isProd ? cdk.RemovalPolicy.RETAIN : cdk.RemovalPolicy.DESTROY,
    });

    refreshTokensTable.addGlobalSecondaryIndex({
      indexName: 'gsi1-userId',
      partitionKey: { name: 'userId', type: dynamodb.AttributeType.STRING },
      projectionType: dynamodb.ProjectionType.ALL,
    });

    // ─── SQS Analytics Queue ─────────────────────────────────────────────────

    const analyticsDlq = new sqs.Queue(this, 'AnalyticsDlq', {
      queueName: `linkguardiao-analytics-dlq-${envName}`,
      retentionPeriod: cdk.Duration.days(14),
      removalPolicy: cdk.RemovalPolicy.DESTROY,
    });

    const analyticsQueue = new sqs.Queue(this, 'AnalyticsQueue', {
      queueName: `linkguardiao-analytics-${envName}`,
      visibilityTimeout: cdk.Duration.seconds(30),
      retentionPeriod: cdk.Duration.days(4),
      deadLetterQueue: {
        queue: analyticsDlq,
        maxReceiveCount: 3,
      },
      removalPolicy: cdk.RemovalPolicy.DESTROY,
    });

    // ─── Lambda Artifacts ────────────────────────────────────────────────────

    const lambdaCodePath = path.join(__dirname, '../../../artifacts/lambda');
    if (!fs.existsSync(lambdaCodePath)) {
      throw new Error(`Lambda artifact not found at ${lambdaCodePath}. Run scripts/aws/build-lambda first.`);
    }

    const consumerCodePath = path.join(__dirname, '../../../artifacts/consumer');
    const consumerExists = fs.existsSync(consumerCodePath);

    // ─── JWT Secret Parameter ────────────────────────────────────────────────

    const jwtSecretParam = new cdk.CfnParameter(this, 'JwtSecret', {
      type: 'String',
      noEcho: true,
      minLength: 32,
    });

    // ─── API Lambda ──────────────────────────────────────────────────────────

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
        DDB_TABLE_REFRESH_TOKENS: refreshTokensTable.tableName,
        JWT__SECRET: jwtSecretParam.valueAsString,
        JWT__ISSUER: 'LinkGuardiao',
        JWT__AUDIENCE: 'LinkGuardiao',
        JWT__ACCESSTOKENMINUTES: '60',
        CORS_ALLOWED_ORIGIN: corsAllowedOrigin,
        LINKLIMITS__DAILYUSERCREATELIMIT: '100',
        SQS_ANALYTICS_QUEUE_URL: analyticsQueue.queueUrl,
      },
    });

    linksTable.grantReadWriteData(apiFunction);
    usersTable.grantReadWriteData(apiFunction);
    accessTable.grantReadWriteData(apiFunction);
    dailyLimitsTable.grantReadWriteData(apiFunction);
    refreshTokensTable.grantReadWriteData(apiFunction);
    analyticsQueue.grantSendMessages(apiFunction);

    apiFunction.addToRolePolicy(new iam.PolicyStatement({
      actions: ['xray:PutTraceSegments', 'xray:PutTelemetryRecords'],
      resources: ['*'],
    }));

    // ─── Analytics Consumer Lambda ───────────────────────────────────────────

    if (consumerExists) {
      const consumerLogGroup = new logs.LogGroup(this, 'ConsumerLogGroup', {
        logGroupName: `/aws/lambda/linkguardiao-consumer-${envName}`,
        retention: logs.RetentionDays.ONE_WEEK,
        removalPolicy: cdk.RemovalPolicy.DESTROY,
      });

      const consumerFunction = new lambda.Function(this, 'ConsumerFunction', {
        functionName: `linkguardiao-consumer-${envName}`,
        runtime: lambda.Runtime.DOTNET_8,
        handler: 'LinkGuardiao.AnalyticsConsumer::LinkGuardiao.AnalyticsConsumer.Function::FunctionHandler',
        code: lambda.Code.fromAsset(consumerCodePath),
        memorySize: 256,
        timeout: cdk.Duration.seconds(30),
        logGroup: consumerLogGroup,
        environment: {
          DDB_TABLE_LINKS: linksTable.tableName,
          DDB_TABLE_ACCESS: accessTable.tableName,
        },
      });

      consumerFunction.addEventSource(new lambda_event_sources.SqsEventSource(analyticsQueue, {
        batchSize: 10,
        reportBatchItemFailures: true,
      }));

      linksTable.grantReadWriteData(consumerFunction);
      accessTable.grantReadWriteData(consumerFunction);

      consumerFunction.addToRolePolicy(new iam.PolicyStatement({
        actions: ['xray:PutTraceSegments', 'xray:PutTelemetryRecords'],
        resources: ['*'],
      }));
    }

    // ─── API Gateway v2 HTTP API ─────────────────────────────────────────────

    const httpApi = new apigwv2.HttpApi(this, 'HttpApi', {
      apiName: `linkguardiao-api-${envName}`,
      corsPreflight: {
        allowOrigins: [corsAllowedOrigin],
        allowMethods: [
          apigwv2.CorsHttpMethod.GET,
          apigwv2.CorsHttpMethod.POST,
          apigwv2.CorsHttpMethod.PUT,
          apigwv2.CorsHttpMethod.DELETE,
          apigwv2.CorsHttpMethod.OPTIONS,
        ],
        allowHeaders: ['Authorization', 'Content-Type', 'Accept', 'X-Request-Id', 'X-Link-Password'],
        maxAge: cdk.Duration.hours(1),
      },
      defaultIntegration: new apigwv2_integrations.HttpLambdaIntegration('ApiIntegration', apiFunction),
    });

    // Stage-level throttling (default stage)
    const cfnStage = httpApi.defaultStage?.node.defaultChild as apigwv2.CfnStage;
    if (cfnStage) {
      cfnStage.defaultRouteSettings = {
        throttlingBurstLimit: 500,
        throttlingRateLimit: 1000,
      };
    }

    // ─── WAF ─────────────────────────────────────────────────────────────────

    const webAcl = new wafv2.CfnWebACL(this, 'WebAcl', {
      name: `linkguardiao-waf-${envName}`,
      scope: 'REGIONAL',
      defaultAction: { allow: {} },
      visibilityConfig: {
        cloudWatchMetricsEnabled: true,
        metricName: `linkguardiao-waf-${envName}`,
        sampledRequestsEnabled: true,
      },
      rules: [
        {
          name: 'AWSManagedRulesCommonRuleSet',
          priority: 1,
          overrideAction: { none: {} },
          statement: {
            managedRuleGroupStatement: {
              vendorName: 'AWS',
              name: 'AWSManagedRulesCommonRuleSet',
            },
          },
          visibilityConfig: {
            cloudWatchMetricsEnabled: true,
            metricName: 'CommonRuleSet',
            sampledRequestsEnabled: true,
          },
        },
        {
          name: 'AWSManagedRulesKnownBadInputsRuleSet',
          priority: 2,
          overrideAction: { none: {} },
          statement: {
            managedRuleGroupStatement: {
              vendorName: 'AWS',
              name: 'AWSManagedRulesKnownBadInputsRuleSet',
            },
          },
          visibilityConfig: {
            cloudWatchMetricsEnabled: true,
            metricName: 'KnownBadInputsRuleSet',
            sampledRequestsEnabled: true,
          },
        },
        {
          name: 'RateLimitPerIp',
          priority: 3,
          action: { block: {} },
          statement: {
            rateBasedStatement: {
              limit: 2000,
              aggregateKeyType: 'IP',
            },
          },
          visibilityConfig: {
            cloudWatchMetricsEnabled: true,
            metricName: 'RateLimitPerIp',
            sampledRequestsEnabled: true,
          },
        },
      ],
    });

    // Associate WAF with API Gateway v2 (HTTP API) stage
    // HTTP API ARN format: arn:{partition}:apigateway:{region}::/apis/{apiId}/stages/$default
    const stageArn = cdk.Fn.join('', [
      'arn:',
      cdk.Aws.PARTITION,
      ':apigateway:',
      cdk.Aws.REGION,
      '::/apis/',
      httpApi.apiId,
      '/stages/$default',
    ]);

    new wafv2.CfnWebACLAssociation(this, 'WebAclAssociation', {
      resourceArn: stageArn,
      webAclArn: webAcl.attrArn,
    });

    // ─── Outputs ─────────────────────────────────────────────────────────────

    new cdk.CfnOutput(this, 'ApiEndpoint', {
      value: httpApi.apiEndpoint,
      description: 'API Gateway HTTP API endpoint URL',
    });
  }
}
