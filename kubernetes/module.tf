terraform {
  required_providers {
    helm = {
      source = "hashicorp/helm"
      version = ">= 2.4.1"
    }
    mcma = {
      source = "ebu/mcma"
      version = "0.0.5"
    }
  }
}

locals {
  values = [yamlencode(merge({
    namespace   = var.kubernetes_namespace
    servicesUrl = var.service_registry.services_url
    service = {
      name             = var.service_name
      type             = var.service_type
      port             = var.service_port
      portName         = var.service_port_name
    }
    mongoDb = {
      connectionString = var.mongodb_connection_string
      databaseName     = var.mongodb_database_name
      collectionName   = var.mongodb_collection_name
    }
    kafka = {
      bootstrapServers = var.kafka_bootstrap_servers
      workerTopic = var.kafka_worker_topic
    }
    apiHandler = {
      dockerImageId = var.api_handler_docker_image_id
      numReplicas = var.api_handler_num_replicas
    }
    worker = {
      dockerImageId = var.worker_docker_image_id
      numReplicas = var.worker_num_replicas
    }
  }, var.helm_values))]
  service_url_scheme   = var.register_https_url ? "https" : "http"
  service_url_hostname = var.service_name
  service_url          = "${local.service_url_scheme}://${local.service_url_hostname}/mcma/api"
}

resource "helm_release" "release" {
  name      = var.service_name
  chart     = "${path.module}/helm/ffmpeg-service"
  values    = local.values
  namespace = var.kubernetes_namespace
}

resource "mcma_job_profile" "extract_thumbnail" {
  name = "ExtractThumbnail"
  
  input_parameter {
    name = "inputFile"
    type = "Locator"
  }
  input_parameter {
    name = "outputLocation"
    type = "Locator"
  }
  input_parameter {
    name     = "ebucore:width"
    type     = "number"
    optional = true
  }
  input_parameter {
    name     = "ebucore:height"
    type     = "number"
    optional = true
  }
  
  output_parameter {
    name = "outputFile"
    type = "Locator"
  }
}

resource "mcma_service" "ffmpeg_service" {
  name     = "FFmpeg Service"
  job_type = "TransformJob"
  
  resource {
    resource_type = "JobAssignment"
    http_endpoint = "${local.service_url}/job-assignments"
  }
  
  job_profile_ids = [
    mcma_job_profile.extract_thumbnail.id
  ]
}