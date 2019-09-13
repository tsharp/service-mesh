FROM microsoft/dotnet:sdk AS build-env
WORKDIR /app

COPY ./*.sln ./ 
# ./NuGet.config 

# Copy the main source project files
COPY */*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p ${file%.*}/ && mv $file ${file%.*}/; done

# Copy the test project files
# COPY test/*/*.csproj ./
# RUN for file in $(ls *.csproj); do mkdir -p test/${file%.*}/ && mv $file test/${file%.*}/; done

# Copy csproj and restore as distinct layers
RUN dotnet restore

# Copy everything else and buildhttps://www.google.com/search?client=ubuntu&channel=fs&q=what+is+my+nat+type&ie=utf-8&oe=utf-8
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM microsoft/dotnet:2.1-aspnetcore-runtime
WORKDIR /app
COPY --from=build-env /app/service-mesh-httptunnel/out .

ENTRYPOINT ["dotnet", "service-mesh-httptunnel.dll"]