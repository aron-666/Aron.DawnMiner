group "default" {
  targets = ["multi-arch"]
}

target "multi-arch" {
  dockerfile = "Dockerfile"
  platforms = ["linux/amd64", "linux/arm64"]
  tags = ["aron666/dawn:latest", "aron666/dawn:1.0.0.9"]
  memory = "8g"
  cpu = "8"
}
