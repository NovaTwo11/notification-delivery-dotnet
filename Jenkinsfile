pipeline {
    agent {
        // Usamos la imagen SDK de .NET 8.0
        docker {
            image 'mcr.microsoft.com/dotnet/sdk:8.0'
        }
    }
    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }
        stage('Restore') {
            steps {
                sh 'dotnet restore'
            }
        }
        stage('Build') {
            steps {
                sh 'dotnet build --no-restore -c Release'
            }
        }
        stage('Test') {
            // Asumiendo que tienes un proyecto de Test (si no, omite este stage)
            steps {
                echo 'Skipping tests...'
                // sh 'dotnet test --no-build -c Release'
            }
        }
        stage('Build Docker Image') {
            steps {
                script {
                    def appImage = docker.build("notification-delivery:${env.BUILD_NUMBER}")
                }
            }
        }
    }
    post {
        always {
            cleanWs()
            // Recoger reportes de tests (si se generan)
            // junit '**/TestResults/*.xml'
        }
    }
}