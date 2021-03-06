locals {
  cosmosdb_id                      = "${var.global_prefix}-db"
  deploy_container_url             = "https://${var.app_storage_account_name}.blob.core.windows.net/${var.deploy_container}"
  ffmpeg_service_api_zip_file      = "./../services/FFmpegService/Mcma.Azure.FFmpegService.ApiHandler/dist/function.zip"
  ffmpeg_service_worker_zip_file   = "./../services/FFmpegService/Mcma.Azure.FFmpegService.Worker/dist/function.zip"
  ffmpeg_service_api_function_name = "${var.global_prefix}-ffmpeg-service-api"
  ffmpeg_service_url               = "https://${local.ffmpeg_service_api_function_name}.azurewebsites.net"
}

resource "azurerm_cosmosdb_sql_container" "ffmpeg_service_cosmosdb_container" {
  name                = "FFmpegService"
  resource_group_name = var.resource_group_name
  account_name        = var.cosmosdb_account_name
  database_name       = var.cosmosdb_db_name
  partition_key_path  = "/partitionKey"
}

#===================================================================
# Worker Function
#===================================================================

resource "azurerm_storage_queue" "ffmpeg_service_worker_function_queue" {
  name                 = "ffmpeg-service-work-queue"
  storage_account_name = var.app_storage_account_name
}

resource "azurerm_storage_blob" "ffmpeg_service_worker_function_zip" {
  name                   = "ffmpeg-service/worker/function_${filesha256(local.ffmpeg_service_worker_zip_file)}.zip"
  storage_account_name   = var.app_storage_account_name
  storage_container_name = var.deploy_container
  type                   = "Block"
  source                 = local.ffmpeg_service_worker_zip_file
}

resource "azurerm_function_app" "ffmpeg_service_worker_function" {
  name                       = "${var.global_prefix}-ffmpeg-service-worker"
  location                   = var.azure_location
  resource_group_name        = var.resource_group_name
  app_service_plan_id        = var.app_service_plan_id
  storage_account_name       = var.app_storage_account_name
  storage_account_access_key = var.app_storage_access_key
  version                    = "~3"

  identity {
    type = "SystemAssigned"
  }

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet"
    FUNCTION_APP_EDIT_MODE         = "readonly"
    https_only                     = true
    HASH                           = filesha256(local.ffmpeg_service_worker_zip_file)
    WEBSITE_RUN_FROM_PACKAGE       = "${local.deploy_container_url}/${azurerm_storage_blob.ffmpeg_service_worker_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = var.appinsights_instrumentation_key

    MCMA_WORK_QUEUE_STORAGE              = var.app_storage_connection_string
    MCMA_TABLE_NAME                      = azurerm_cosmosdb_sql_container.ffmpeg_service_cosmosdb_container.name
    MCMA_PUBLIC_URL                      = local.ffmpeg_service_url
    MCMA_COSMOS_DB_ENDPOINT              = var.cosmosdb_endpoint
    MCMA_COSMOS_DB_KEY                   = var.cosmosdb_key
    MCMA_COSMOS_DB_DATABASE_ID           = local.cosmosdb_id
    MCMA_COSMOS_DB_REGION                = var.azure_location
    MCMA_SERVICES_URL                    = var.services_url
    MCMA_SERVICES_AUTH_TYPE              = var.service_registry_auth_type
    MCMA_SERVICES_AUTH_CONTEXT           = var.service_registry_auth_context
    MCMA_MEDIA_STORAGE_ACCOUNT_NAME      = var.media_storage_account_name
    MCMA_MEDIA_STORAGE_CONNECTION_STRING = var.media_storage_connection_string
  }

  provisioner "local-exec" {
    command = "az webapp start --resource-group ${var.resource_group_name} --name ${azurerm_function_app.ffmpeg_service_worker_function.name}"
  }
}

#===================================================================
# API Function
#===================================================================

resource "azuread_application" "ffmpeg_service_app" {
  name            = local.ffmpeg_service_api_function_name
  identifier_uris = [local.ffmpeg_service_url]
}

resource "azuread_service_principal" "ffmpeg_service_sp" {
  application_id               = azuread_application.ffmpeg_service_app.application_id
  app_role_assignment_required = false
}

resource "azurerm_storage_blob" "ffmpeg_service_api_function_zip" {
  name                   = "ffmpeg-service/api/function_${filesha256(local.ffmpeg_service_api_zip_file)}.zip"
  storage_account_name   = var.app_storage_account_name
  storage_container_name = var.deploy_container
  type                   = "Block"
  source                 = local.ffmpeg_service_api_zip_file
}

resource "azurerm_function_app" "ffmpeg_service_api_function" {
  name                       = local.ffmpeg_service_api_function_name
  location                   = var.azure_location
  resource_group_name        = var.resource_group_name
  app_service_plan_id        = var.app_service_plan_id
  storage_account_name       = var.app_storage_account_name
  storage_account_access_key = var.app_storage_access_key
  version                    = "~3"

  auth_settings {
    enabled                       = true
    issuer                        = "https://sts.windows.net/${var.azure_tenant_id}"
    default_provider              = "AzureActiveDirectory"
    unauthenticated_client_action = "RedirectToLoginPage"
    active_directory {
      client_id         = azuread_application.ffmpeg_service_app.application_id
      allowed_audiences = [local.ffmpeg_service_url]
    }
  }

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet"
    FUNCTION_APP_EDIT_MODE         = "readonly"
    https_only                     = true
    HASH                           = filesha256(local.ffmpeg_service_api_zip_file)
    WEBSITE_RUN_FROM_PACKAGE       = "${local.deploy_container_url}/${azurerm_storage_blob.ffmpeg_service_api_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = var.appinsights_instrumentation_key

    MCMA_TABLE_NAME            = azurerm_cosmosdb_sql_container.ffmpeg_service_cosmosdb_container.name
    MCMA_PUBLIC_URL            = local.ffmpeg_service_url
    MCMA_COSMOS_DB_ENDPOINT    = var.cosmosdb_endpoint
    MCMA_COSMOS_DB_KEY         = var.cosmosdb_key
    MCMA_COSMOS_DB_DATABASE_ID = local.cosmosdb_id
    MCMA_COSMOS_DB_REGION      = var.azure_location
    MCMA_WORKER_QUEUE_NAME     = azurerm_storage_queue.ffmpeg_service_worker_function_queue.name
  }
}
