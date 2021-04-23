output "auth_type" {
   value = "AWS4"
}

output "job_assignments_url" {
   value = "https://${aws_api_gateway_rest_api.ffmpeg_service_api.id}.execute-api.${var.aws_region}.amazonaws.com/${var.environment_type}/job-assignments"
}