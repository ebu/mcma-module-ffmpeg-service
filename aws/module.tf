locals {
  functions_dir        = "${path.module}/functions"
  layer_zip_file       = "./layers/ffmpeg.zip"
  api_handler_zip_file = "${local.functions_dir}/api-handler.zip"
  worker_zip_file      = "${local.functions_dir}/worker.zip"
  service_url          = "${aws_apigatewayv2_api.service_api.api_endpoint}/${var.stage_name}"
  worker_lambda_name   = format("%.64s", replace("${var.name}-worker", "/[^a-zA-Z0-9_]+/", "-" ))
}

##################################
# aws_iam_role + aws_iam_policy
##################################

resource "aws_iam_role" "lambda_execution" {
  name               = format("%.64s", "${var.name}.${var.aws_region}.lambda-execution")
  path               = var.iam_role_path
  assume_role_policy = jsonencode({
    Version   = "2012-10-17",
    Statement = [
      {
        Sid       = "AllowLambdaAssumingRole"
        Effect    = "Allow"
        Action    = "sts:AssumeRole",
        Principal = {
          "Service" = "lambda.amazonaws.com"
        }
      }
    ]
  })

  tags = var.tags
}

resource "aws_iam_policy" "lambda_execution" {
  name        = format("%.128s", "${var.name}.${var.aws_region}.lambda-execution")
  description = "Policy to write to log"
  path        = var.iam_policy_path
  policy      = jsonencode({
    Version   = "2012-10-17",
    Statement = concat([
      {
        Sid      = "AllowLambdaWritingToLogs"
        Effect   = "Allow",
        Action   = "logs:*",
        Resource = "*"
      },
      {
        Sid      = "ListAndDescribeDynamoDBTables",
        Effect   = "Allow",
        Action   = [
          "dynamodb:List*",
          "dynamodb:DescribeReservedCapacity*",
          "dynamodb:DescribeLimits",
          "dynamodb:DescribeTimeToLive"
        ],
        Resource = "*"
      },
      {
        Sid      = "SpecificTable",
        Effect   = "Allow",
        Action   = [
          "dynamodb:BatchGet*",
          "dynamodb:DescribeStream",
          "dynamodb:DescribeTable",
          "dynamodb:Get*",
          "dynamodb:Query",
          "dynamodb:Scan",
          "dynamodb:BatchWrite*",
          "dynamodb:CreateTable",
          "dynamodb:Delete*",
          "dynamodb:Update*",
          "dynamodb:PutItem"
        ],
        Resource = [
          aws_dynamodb_table.service_table.arn,
          "${aws_dynamodb_table.service_table.arn}/index/*"
        ]
      },
      {
        Sid      = "AllowInvokingWorkerLambda",
        Effect   = "Allow",
        Action   = "lambda:InvokeFunction",
        Resource = "arn:aws:lambda:${var.aws_region}:${var.aws_account_id}:function:${local.worker_lambda_name}"
      },
      {
        Sid      = "AllowInvokingApiGateway"
        Effect   = "Allow",
        Action   = "execute-api:Invoke",
        Resource = "arn:aws:execute-api:*:*:*"
      }
    ],
    var.xray_tracing_enabled ?
    [{
      Sid      = "AllowLambdaWritingToXRay"
      Effect   = "Allow",
      Action   = [
        "xray:PutTraceSegments",
        "xray:PutTelemetryRecords"
      ],
      Resource = "*"
    }]: [],
    var.dead_letter_config_target != null ?
    [{
      Effect: "Allow",
      Action: "sqs:SendMessage",
      Resource: var.dead_letter_config_target
    }] : [])
  })
}

resource "aws_iam_role_policy_attachment" "lambda_execution" {
  role       = aws_iam_role.lambda_execution.id
  policy_arn = aws_iam_policy.lambda_execution.arn
}

##################################
# aws_dynamodb_table
##################################

resource "aws_dynamodb_table" "service_table" {
  name         = var.name
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "resource_type"
  range_key    = "resource_id"

  attribute {
    name = "resource_type"
    type = "S"
  }

  attribute {
    name = "resource_id"
    type = "S"
  }

  stream_enabled   = true
  stream_view_type = "NEW_IMAGE"
}

#################################
#  aws_lambda_function : api_handler
#################################

resource "aws_lambda_function" "api_handler" {
  depends_on = [
    aws_iam_role_policy_attachment.lambda_execution
  ]
  
  filename         = local.api_handler_zip_file
  function_name    = format("%.64s", replace("${var.name}-api-handler", "/[^a-zA-Z0-9_]+/", "-"))
  role             = aws_iam_role.lambda_execution.arn
  handler          = "Mcma.Modules.FFmpegService.Aws.ApiHandler::Mcma.Modules.FFmpegService.Aws.ApiHandler.FFmpegServiceApiHandler::ExecuteAsync"
  source_code_hash = filebase64sha256(local.api_handler_zip_file)
  runtime          = "dotnetcore3.1"
  timeout          = "30"
  memory_size      = "3008"

  environment {
    variables = {
      MCMA_LOG_GROUP_NAME              = var.log_group.name
      MCMA_TABLE_NAME                  = aws_dynamodb_table.service_table.name
      MCMA_PUBLIC_URL                  = local.service_url
      MCMA_SERVICES_URL                = var.service_registry.services_url
      MCMA_SERVICES_AUTH_TYPE          = var.service_registry.auth_type
      MCMA_WORKER_LAMBDA_FUNCTION_NAME = aws_lambda_function.worker.function_name
    }
  }
}

#################################
#  aws_lambda_function : worker
#################################

resource "aws_lambda_layer_version" "ffmpeg" {
  filename         = local.layer_zip_file
  layer_name       = "${var.name}-ffmpeg"
  source_code_hash = filebase64sha256(local.layer_zip_file)
}

resource "aws_lambda_function" "worker" {
  filename         = local.worker_zip_file
  function_name    = local.worker_lambda_name
  role             = aws_iam_role.lambda_execution.arn
  handler          = "Mcma.Modules.FFmpegService.Aws.Worker::Mcma.Modules.FFmpegService.Aws.Worker.FFmpegServiceWorker::ExecuteAsync"
  source_code_hash = filebase64sha256(local.worker_zip_file)
  runtime          = "dotnetcore3.1"
  timeout          = "900"
  memory_size      = "3008"

  layers = [aws_lambda_layer_version.ffmpeg.arn]

  environment {
    variables = {
      MCMA_LOG_GROUP_NAME     = var.log_group.name
      MCMA_TABLE_NAME         = aws_dynamodb_table.service_table.name
      MCMA_PUBLIC_URL         = local.service_url
      MCMA_SERVICES_URL       = var.service_registry.services_url
      MCMA_SERVICES_AUTH_TYPE = var.service_registry.auth_type
    }
  }
}

##############################
#  aws_api_gateway_rest_api:  ffmpeg_service_api
##############################
resource "aws_apigatewayv2_api" "service_api" {
  name          = var.name
  description   = "FFmpeg Service REST API"
  protocol_type = "HTTP"
  
  tags = var.tags
}

resource "aws_apigatewayv2_integration" "service_api" {
  api_id                 = aws_apigatewayv2_api.service_api.id
  connection_type        = "INTERNET"
  integration_method     = "POST"
  integration_type       = "AWS_PROXY"
  integration_uri        = aws_lambda_function.api_handler.arn
  payload_format_version = "2.0"
}

resource "aws_apigatewayv2_route" "service_api_default" {
  api_id             = aws_apigatewayv2_api.service_api.id
  route_key          = "$default"
  authorization_type = "AWS_IAM"
  target             = "integrations/${aws_apigatewayv2_integration.service_api.id}"
}

resource "aws_lambda_permission" "service_api_default" {
  statement_id  = "AllowExecutionFromAPIGatewayDefault"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.api_handler.arn
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.service_api.execution_arn}/*/$default"
}

resource "aws_apigatewayv2_stage" "service_api" {
  depends_on = [
    aws_apigatewayv2_route.service_api_default
  ]

  api_id      = aws_apigatewayv2_api.service_api.id
  name        = var.stage_name
  auto_deploy = true

  default_route_settings {
    data_trace_enabled       = var.xray_tracing_enabled
    detailed_metrics_enabled = var.api_gateway_metrics_enabled
    logging_level            = var.api_gateway_logging_enabled ? "INFO" : null
    throttling_burst_limit   = 5000
    throttling_rate_limit    = 10000
  }

  access_log_settings {
    destination_arn = var.log_group.arn
    format          = "{ \"requestId\":\"$context.requestId\", \"ip\": \"$context.identity.sourceIp\", \"requestTime\":\"$context.requestTime\", \"httpMethod\":\"$context.httpMethod\",\"routeKey\":\"$context.routeKey\", \"status\":\"$context.status\",\"protocol\":\"$context.protocol\", \"responseLength\":\"$context.responseLength\" }"
  }

  tags = var.tags
}
