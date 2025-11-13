# glTFast Quality Strategy

## Quality Objective

- Defect Rate: Ensure that the defect rate remains minimal.
- Test Coverage: Achieve and maintain at least 70% test coverage across the entire codebase.
- Automated Testing: Implement and maintain automated tests for all new features.
- Manual Testing: Conduct manual tests for complex features, edge cases, and release validation.
- Documentation: Maintain comprehensive and up-to-date documentation for all testing procedures and results.

## Quality Standards and Metrics

### Standards

- Code Style: Adhere to the established coding standards and guidelines from Unity.
- Automation: Prioritize automated testing to ensure consistent and repeatable test execution.

### Metrics

- Automated test pass rate must be 100% on all supported platforms and Unity versions.
- The package should be imported by users without issues or errors.
- Documentation must be clear and easy to follow.

## Quality Process and Practices

- Code Reviews: Implement a mandatory code review process for all changes to the codebase.
- Automated Testing: Integrate automated tests into the CI/CD pipeline to ensure that tests are run on every code change.
- Manual Testing: Define and document manual testing procedures for complex features and release validation.
- Graphics Testing: Implement graphics tests to ensure visual fidelity and performance across different hardware configurations.
- Documentation Checks: Regularly review and update documentation to ensure accuracy and completeness.

----

## Current State of glTFast Quality Strategy

This section outlines the current state and future plans for the testing strategy of the glTFast project. It covers various phases of development, including pipelines, automated tests, manual tests, and the release process.

### Current Situation

|          |                                                                                                                                                                                                                                                                                                                                                                    |
|------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Pipelines and CI | - CI pipelines in place, composed of multiple tests on multiple levels of the dev cycle.                                                                                                                                                                                                                                                                           |
| Automated tests  | - Automated tests run across multiple dev phases.  - Unit tests with NUnit cover individual components and functions.  - Editor, Runtime and validation tests are run on every PR.  - Code Coverage and Code formatting is run on every PR.  - Weekly and Nightly tests running on multiple platforms and Unity versions. (More explanation in the section below). |
| Manual tests     | - Minimal manual test is made right now and described in [Release Manual Validation](./Release-Manual-Validation.md) . The rest is yet to be defined.                                                                                                                                                                                                              |
| Release process  | - The release process is defined in [Release Process](../Release-Process/index.md).                                                                                                                                                                                                                                                                             |

### Continuous Integration summary

This section outlines the Continuous Integration (CI) setup for the glTFast project.
Jobs are ran on major platforms and Unity minimal version is xLTS.

| Job Type        | Frequency                        |
|-----------------|----------------------------------|
| Editor Tests    | Pull Request, Weekly             |
| Runtime Tests   | Pull Request, Weekly             |
| Graphics Tests  | Manually triggered               |
| API Validation  | Pull Request, Weekly             |
| Code Coverage   | Pull Request, Weekly, Post Merge |
| Code Formatting | Pull Request, Weekly             |
