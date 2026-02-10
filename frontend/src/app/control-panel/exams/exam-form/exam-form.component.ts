import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { FormShellComponent } from '../../../shared/components/form-shell/form-shell.component';

interface ExamQuestion {
  text: string;
  choices: string[];
  correctChoiceIndex: number;
}

@Component({
  selector: 'app-exam-form',
  standalone: true,
  imports: [CommonModule, FormsModule, FormShellComponent],
  templateUrl: './exam-form.component.html',
  styleUrl: './exam-form.component.css'
})
export class ExamFormComponent implements OnInit {
  isEditMode = false;
  examId: string | null = null;
  
  exam = {
    title: '',
    passThreshold: 70,
    questions: [] as ExamQuestion[]
  };

  pristine = true;
  submitting = false;
  backendErrors: any = null;

  constructor(
    private apiService: ApiService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.examId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.examId;

    if (this.isEditMode && this.examId) {
      this.loadExam();
    } else {
      // Initialize with one empty question
      this.addQuestion();
    }
  }

  loadExam(): void {
    if (!this.examId) return;

    this.apiService.get(`api/exams/${this.examId}`)
      .subscribe({
        next: (data: any) => {
          this.exam = {
            title: data.title || '',
            passThreshold: data.passThreshold || 70,
            questions: data.questions || []
          };
        },
        error: (err) => {
          this.backendErrors = { general: err.message || 'Failed to load exam' };
        }
      });
  }

  addQuestion(): void {
    this.exam.questions.push({
      text: '',
      choices: ['', '', '', ''],
      correctChoiceIndex: 0
    });
    this.pristine = false;
  }

  removeQuestion(index: number): void {
    this.exam.questions.splice(index, 1);
    this.pristine = false;
  }

  addChoice(questionIndex: number): void {
    this.exam.questions[questionIndex].choices.push('');
    this.pristine = false;
  }

  removeChoice(questionIndex: number, choiceIndex: number): void {
    const question = this.exam.questions[questionIndex];
    question.choices.splice(choiceIndex, 1);
    
    // Adjust correctChoiceIndex if needed
    if (question.correctChoiceIndex >= question.choices.length) {
      question.correctChoiceIndex = Math.max(0, question.choices.length - 1);
    }
    
    this.pristine = false;
  }

  onFieldChange(): void {
    this.pristine = false;
  }

  onSubmit(): void {
    if (this.submitting) return;

    this.submitting = true;
    this.backendErrors = null;

    const request = this.isEditMode
      ? this.apiService.put(`api/exams/${this.examId}`, this.exam)
      : this.apiService.post('api/exams', this.exam);

    request.subscribe({
      next: () => {
        this.router.navigate(['/control-panel/exams']);
      },
      error: (err) => {
        this.backendErrors = err.error || { general: err.message || 'Failed to save exam' };
        this.submitting = false;
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/control-panel/exams']);
  }

  get isDirty(): boolean {
    return !this.pristine;
  }

  trackByIndex(index: number): number {
    return index;
  }
}
