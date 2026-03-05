#!/usr/bin/env node
import * as cdk from 'aws-cdk-lib';
import { LinkGuardiaoStack } from '../lib/linkguardiao-stack';

const app = new cdk.App();
const envName = app.node.tryGetContext('env') ?? 'dev';
const corsAllowedOrigin = app.node.tryGetContext('corsAllowedOrigin');

new LinkGuardiaoStack(app, `LinkGuardiao-${envName}`, {
  envName,
  corsAllowedOrigin,
  env: {
    account: process.env.CDK_DEFAULT_ACCOUNT,
    region: process.env.CDK_DEFAULT_REGION,
  },
});
