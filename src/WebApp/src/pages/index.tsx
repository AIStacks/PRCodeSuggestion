import React, { useState } from "react";
import * as signalR from "@microsoft/signalr";
import { createFileRoute } from "@tanstack/react-router";
import { PRCodeImprovement } from "../types/codeSuggestion";
import {
  Container,
  Row,
  Col,
  Form,
  Button,
  Alert,
  Accordion,
  Table,
  Spinner,
} from "react-bootstrap";
import { Formik } from "formik";
import * as Yup from "yup";
import { FaCopy } from "react-icons/fa";
import { Result } from "../types/result";
import { Helmet } from "react-helmet-async";

const schema = Yup.object().shape({
  url: Yup.string().required("Required"),
});

const Loader = () => {
  return (
    <Spinner animation="border" role="status" className="mt-3">
      <span className="sr-only"></span>
    </Spinner>
  );
};

const LiveUpdates: React.FC = () => {
  const [records, setRecords] = useState<PRCodeImprovement[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [url, setUrl] = useState("");

  const onPost = (values: { url: string }) => {
    setRecords([]);
    setError(null);

    // Set up SignalR connection
    const connection = new signalR.HubConnectionBuilder()
      .withUrl("/CodeSuggestionHub") // Ensure this matches your server endpoint
      .withAutomaticReconnect()
      .build();

    connection.serverTimeoutInMilliseconds = 30 * 60 * 1000;
    connection.keepAliveIntervalInMilliseconds = 30 * 60 * 1000;

    // Start connection and handle streaming
    connection!
      .start()
      .then(() => {
        console.log("Connected to the SignalR hub");

        // Store connection ID
        const id = connection!.connectionId;
        if (id) {
          // Subscribe to the streaming method from the hub
          connection!
            .stream("StreamUpdatesToClient", id, values.url)
            .subscribe({
              next: (update: Result<PRCodeImprovement>) => {
                // Update the state with new data
                if (update.isSuccess) {
                  setRecords((prevUpdates) => [...prevUpdates, update.value!]);
                } else {
                  setError(update.errors.join("\n"));
                }
              },
              complete: () => {
                console.log("Stream completed");
                setSubmitting(false);
                connection
                  .stop()
                  .then(() => console.log("Disconnected from the hub"));
              },
              error: (err) => {
                setError(`Stream error: ${err}`);
                connection
                  .stop()
                  .then(() => console.log("Disconnected from the hub"));
                setSubmitting(false);
              },
            });
        }
      })
      .catch((err) => {
        setError(`Connection failed: ${err}`);
        setSubmitting(false);
      });
  };

  return (
    <>
      <Helmet>
        <title>AI Agents - PR Code Suggestion</title>
      </Helmet>
      <Container className="mt-5">
        <Formik
          enableReinitialize={true}
          initialValues={{ url: "" }}
          validationSchema={schema}
          onSubmit={(values, actions) => {
            setSubmitting(true);
            setUrl(values.url);
            setTimeout(() => {
              const castedValues = schema.cast(values);
              onPost(castedValues);
              actions.setSubmitting(false);
            }, 10);
          }}
        >
          {({ handleSubmit, handleChange, handleBlur, values, errors }) => (
            <Form onSubmit={handleSubmit}>
              <Row className="flex-nowrap">
                <Col>
                  <Form.Control
                    type="url"
                    placeholder="Pull Request Url"
                    onChange={handleChange}
                    onBlur={handleBlur}
                    value={values["url"]}
                    isInvalid={!!errors["url"]}
                    name="url"
                    autoComplete="off"
                  />
                </Col>
                <Col xs="auto">
                  <Button
                    variant="primary"
                    className="h-100 py-1"
                    type="submit"
                    disabled={submitting}
                  >
                    Submit
                  </Button>
                </Col>
              </Row>
            </Form>
          )}
        </Formik>
      </Container>
      <hr />
      <Container>
        {error && (
          <Row>
            <Col>
              <Alert variant="danger">{error}</Alert>
            </Col>
          </Row>
        )}
        <Row>
          <Col>
            <Accordion defaultActiveKey="0">
              {records &&
                records.map((record, index) => (
                  <Accordion.Item eventKey={`${index}`} key={index}>
                    <Accordion.Header><a href={`${url}?_a=files&path=${record.relevant_file}`} target="_blank" onClick={e => e.stopPropagation()}>{record.relevant_file}</a></Accordion.Header>
                    <Accordion.Body>
                      <Table striped bordered hover>
                        <thead>
                          <tr>
                            <th>#</th>
                            <th>Category</th>
                            <th>Suggestion</th>
                          </tr>
                        </thead>
                        <tbody>
                          {record.code_suggestions.map((suggestion, key) => {
                            return (
                              <tr key={key}>
                                <td>{key + 1}</td>
                                <td>{suggestion.label}</td>
                                <td>
                                  <Accordion flush>
                                    <Accordion.Item eventKey="0">
                                      <Accordion.Header>
                                        {suggestion.one_sentence_summary}
                                      </Accordion.Header>
                                      <Accordion.Body>
                                        <Container fluid className="mx-2">
                                          <Row>
                                            <Col className="mb-3">
                                              {suggestion.suggestion_content}
                                            </Col>
                                          </Row>
                                          <Row>
                                            <Col className="mb-2">
                                              {suggestion.relevant_lines_start !==
                                                suggestion.relevant_lines_end ? (
                                                <>
                                                  <h6 className="d-inline me-2">
                                                    Lines:
                                                  </h6>
                                                  {`[${suggestion.relevant_lines_start} - ${suggestion.relevant_lines_end}]`}
                                                </>
                                              ) : (
                                                <>
                                                  <h6 className="d-inline me-2">
                                                    Line:
                                                  </h6>
                                                  {`${suggestion.relevant_lines_start}`}
                                                </>
                                              )}
                                            </Col>
                                          </Row>
                                          <Row>
                                            <Col className="mb-2">
                                              <h6>Existing code:</h6>
                                              <div className="p-2 bg-light">
                                                <code>
                                                  <pre>
                                                    {suggestion.existing_code}
                                                  </pre>
                                                </code>
                                              </div>
                                            </Col>
                                          </Row>
                                          <Row>
                                            <Col className="mb-2">
                                              <h6>Suggested change:</h6>

                                              <div className="position-relative p-2 bg-light overflow-x-auto">
                                                <Button
                                                  className="position-absolute top-0 end-0 m-2"
                                                  size="sm"
                                                  variant="outline-secondary"
                                                  onClick={() => {
                                                    navigator.clipboard.writeText(
                                                      `${suggestion.improved_code}`,
                                                    );
                                                    alert("Code copied!");
                                                  }}
                                                >
                                                  <FaCopy />
                                                </Button>
                                                <code>
                                                  <pre>
                                                    {suggestion.improved_code}
                                                  </pre>
                                                </code>
                                              </div>
                                            </Col>
                                          </Row>
                                        </Container>
                                      </Accordion.Body>
                                    </Accordion.Item>
                                  </Accordion>
                                </td>
                              </tr>
                            );
                          })}
                        </tbody>
                      </Table>
                    </Accordion.Body>
                  </Accordion.Item>
                ))}
            </Accordion>
          </Col>
        </Row>
        {submitting && (
          <Row>
            <Col>
              <Loader />
            </Col>
          </Row>
        )}
      </Container>
    </>
  );
};

export const Route = createFileRoute("/")({
  component: LiveUpdates,
});
