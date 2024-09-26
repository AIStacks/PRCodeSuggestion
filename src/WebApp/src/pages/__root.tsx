import { createRootRoute, Outlet } from "@tanstack/react-router";
import React from "react";
import { HelmetProvider } from "react-helmet-async";
import { Col, Container, Row } from "react-bootstrap";

export const Route = createRootRoute({
  component: React.memo(() => {
    return (
      <HelmetProvider>
        <Outlet />
      </HelmetProvider>
    );
  }),
  notFoundComponent: () => {
    return (
        <Container>
          <Row>
            <Col className="w-100">
              <h1 className="my-5">Not found!</h1>
            </Col>
          </Row>
        </Container>
    );
  },
});
