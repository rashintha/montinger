FROM golang:1.24 AS build
WORKDIR /src
COPY . .
RUN CGO_ENABLED=0 go build -o /app/agent .

FROM gcr.io/distroless/base-debian12
WORKDIR /app
COPY --from=build /app/agent .
ENTRYPOINT ["/app/agent"]