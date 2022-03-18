variable "service_name" {
    default = "ffmpeg-service"
}
variable "service_type" {
    default = "ClusterIP"
}
variable "service_port" {
    default = 80
}
variable "service_port_name" {
    default = "api"
}

variable "register_https_url" {
    default = true
}

variable "service_registry" {
    type = object({
        auth_context = string,
        auth_type    = string,
        services_url = string,
    })
}

variable "kubernetes_namespace" {
    default = "default"
}

variable "mongodb_connection_string" {
    type = string
}
variable "mongodb_database_name" {
    default = "mcma"
}
variable "mongodb_collection_name" {
    default = "ffmpeg-service"
}

variable "kafka_bootstrap_servers" {
    type = string
}
variable "kafka_worker_topic" {
    default = "mcma.ffmpegservice.worker"
}

variable "api_handler_docker_image_id" {
    default = "evanverneyfink/mcma-ffmpeg-service-api"
}
variable "api_handler_num_replicas" { 
    default = 1
}

variable "worker_docker_image_id" {
    default = "evanverneyfink/mcma-ffmpeg-service-worker"
}
variable "worker_num_replicas" {
    default = 1
}

variable "helm_values" {
    type    = map(any)
    default = {}
}