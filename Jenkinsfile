// Jenkinsfile para notification-delivery-dotnet

pipeline {
    // Usamos 'agent any' porque tu Jenkins ya tiene acceso al 
    // socket de Docker del host (según tu docker-compose.yml)
    agent any 

    environment {
        // Nombre de la imagen que construiremos
        IMAGE_NAME = 'notification-delivery'
        
        // Tag (usamos el número de build de Jenkins)
        IMAGE_TAG = "${env.BUILD_NUMBER}"
        
        // Contenedor SDK para usar en la etapa de pruebas
        DOTNET_SDK_IMAGE = 'mcr.microsoft.com/dotnet/sdk:8.0'
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm // Clona tu código
            }
        }

        stage('Pruebas Unitarias (Tests)') {
            steps {
                script {
                    docker.image(DOTNET_SDK_IMAGE).inside {
                        // --- LA CORRECCIÓN ---
                        // Le decimos explícitamente qué proyecto restaurar
                        sh 'dotnet restore NotificationDelivery.csproj'
                        
                        // También corregí el ejemplo de 'dotnet test' que tenías comentado,
                        // por si lo usas en el futuro, para que apunte al proyecto.
                        // sh 'dotnet test NotificationDelivery.csproj --no-restore --logger "trx;LogFileName=testresults.trx" /p:CollectCoverage=true /p:CoverletOutputFormat=opencover'
                        echo 'Saltando pruebas (puedes configurar tu proyecto de tests aquí)...'
                    }
                }
            }
        }

        stage('Construir Imagen Docker') {
            steps {
                script {
                    // Le decimos a Jenkins que construya el Dockerfile 
                    // que está en el directorio actual ('.')
                    // El Dockerfile Multi-stage se encargará de compilar, publicar Y crear la imagen final.
                    def appImage = docker.build("${IMAGE_NAME}:${IMAGE_TAG}", '.')
                }
            }
        }

        stage('Push a Registry (Opcional)') {
            // Esta etapa solo se ejecuta si el build es en la rama 'main' o 'master'
            when { branch 'main' }
            
            steps {
                echo "Haciendo 'push' de ${IMAGE_NAME}:${IMAGE_TAG}..."
                // Aquí necesitarías tus credenciales (ej. 'dockerhub-credentials')
                // guardadas en Jenkins
                //
                // docker.withRegistry('https://index.docker.io/v1/', 'dockerhub-credentials') {
                //    docker.image(IMAGE_NAME).push(IMAGE_TAG)
                // }
                
                echo "Simulación de push (configura tu registry aquí)..."
            }
        }
    }

    post {
        // Se ejecuta siempre, al final del pipeline
        always {
            echo 'Pipeline finalizado.'
            // Opcional: Limpiar la imagen construida si no se hizo push
            // sh "docker rmi ${IMAGE_NAME}:${IMAGE_TAG}"
        }
    }
}