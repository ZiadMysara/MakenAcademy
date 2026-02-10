import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ApiService } from './api.service';
import { environment } from '../../../environments/environment';

describe('ApiService', () => {
  let service: ApiService;
  let httpMock: HttpTestingController;
  const baseUrl = environment.apiBaseUrl; // Use actual environment config

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ApiService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(ApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify(); // Verify no outstanding HTTP requests
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('GET requests', () => {
    it('should make a GET request to the correct URL', () => {
      const mockResponse = { data: 'test' };
      const endpoint = 'api/test';

      service.get(endpoint).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${baseUrl}/${endpoint}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should handle GET request errors', () => {
      const endpoint = 'api/test';

      service.get(endpoint).subscribe({
        next: () => {
          throw new Error('should have failed with 404 error');
        },
        error: (error) => {
          expect(error.message).toContain('Resource not found');
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/${endpoint}`);
      req.flush('Not Found', { status: 404, statusText: 'Not Found' });
    });
  });

  describe('POST requests', () => {
    it('should make a POST request with body', () => {
      const mockResponse = { id: '123' };
      const endpoint = 'api/test';
      const body = { name: 'test' };

      service.post(endpoint, body).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${baseUrl}/${endpoint}`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(body);
      req.flush(mockResponse);
    });

    it('should handle POST request errors', () => {
      const endpoint = 'api/test';
      const body = { name: 'test' };

      service.post(endpoint, body).subscribe({
        next: () => {
          throw new Error('should have failed with 400 error');
        },
        error: (error) => {
          expect(error.message).toContain('Bad request');
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/${endpoint}`);
      req.flush('Bad Request', { status: 400, statusText: 'Bad Request' });
    });
  });

  describe('PUT requests', () => {
    it('should make a PUT request with body', () => {
      const mockResponse = { updated: true };
      const endpoint = 'api/test/123';
      const body = { name: 'updated' };

      service.put(endpoint, body).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${baseUrl}/${endpoint}`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(body);
      req.flush(mockResponse);
    });
  });

  describe('DELETE requests', () => {
    it('should make a DELETE request', () => {
      const endpoint = 'api/test/123';

      service.delete(endpoint).subscribe(response => {
        expect(response).toEqual({});
      });

      const req = httpMock.expectOne(`${baseUrl}/${endpoint}`);
      expect(req.request.method).toBe('DELETE');
      req.flush({});
    });
  });

  describe('Error handling', () => {
    it('should handle 401 Unauthorized errors', () => {
      const endpoint = 'api/test';

      service.get(endpoint).subscribe({
        next: () => {
          throw new Error('should have failed with 401 error');
        },
        error: (error) => {
          expect(error.message).toContain('Unauthorized');
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/${endpoint}`);
      req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    });

    it('should handle 403 Forbidden errors', () => {
      const endpoint = 'api/test';

      service.get(endpoint).subscribe({
        next: () => {
          throw new Error('should have failed with 403 error');
        },
        error: (error) => {
          expect(error.message).toContain('Forbidden');
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/${endpoint}`);
      req.flush('Forbidden', { status: 403, statusText: 'Forbidden' });
    });

    it('should handle 500 Server errors', () => {
      const endpoint = 'api/test';

      service.get(endpoint).subscribe({
        next: () => {
          throw new Error('should have failed with 500 error');
        },
        error: (error) => {
          expect(error.message).toContain('Server error');
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/${endpoint}`);
      req.flush('Internal Server Error', { status: 500, statusText: 'Internal Server Error' });
    });

    it('should handle network errors', () => {
      const endpoint = 'api/test';

      service.get(endpoint).subscribe({
        next: () => {
          throw new Error('should have failed with network error');
        },
        error: (error) => {
          // Network errors result in status 0
          expect(error.message).toContain('Error 0');
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/${endpoint}`);
      req.error(new ProgressEvent('Network error'));
    });
  });
});
